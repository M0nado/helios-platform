using HELIOS.Analytics.FSharp;
using HELIOS.Platform.Contracts.XCore9;
using HELIOS.XCore9;
using Xunit;

namespace HELIOS.XCore9.Tests;
public sealed class XCore9ServiceTests
{
    [Fact] public async Task Selection_rejects_unknown_templates_and_enforces_limits()
    {
        var service=Create(new XCore9Options(MaxTotalInstances:1,MaxCpuUnits:2,MaxMemoryMiB:512));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async()=>await service.SelectWorkerAsync("generated","safe","c-1","operator",default));
        var lease=await service.SelectWorkerAsync("reviewer","safe","c-1","operator",default);
        await Assert.ThrowsAsync<InvalidOperationException>(async()=>await service.SelectWorkerAsync("reviewer","safe","c-2","operator",default));
        service.ReleaseWorker(lease);
    }
    [Fact] public void Feature_extraction_removes_unknown_non_finite_and_bounds_values()
    {
        var service=Create(); var entry=new RunHistoryEntry(Guid.NewGuid(),"c","reviewer",true,TimeSpan.Zero,0,[new("success_rate",2),new("raw_prompt",1),new("cost_ratio",double.NaN)],[]);
        var feature=Assert.Single(service.ExtractFeatures(entry)); Assert.Equal("success_rate",feature.Name); Assert.Equal(1,feature.Value);
    }
    [Fact] public void Toolchains_are_dependency_ordered_and_closed_over_approved_tools()
    { var ordered=Create().ConstructToolchain("safe").OrderedToolIds; Assert.Equal(new[]{"read","test"},ordered); }
    [Fact] public async Task Promotion_requires_holdout_external_authority_and_preserves_rollback()
    {
        var service=Create(); var candidate=new RoutingPolicy("candidate",2,new Dictionary<string,string>(),null); var eval=new HoldoutEvaluation(100,.4,.2,.9,true);
        var self=await service.EvaluatePromotionAsync(new(candidate,eval,"xcore-9",[new Uri("https://evidence.test/1")]),"xcore-9",default); Assert.False(self.Approved);
        var approved=await service.EvaluatePromotionAsync(new(candidate,eval,"guardian",[new Uri("https://evidence.test/1")]),"guardian",default); Assert.True(approved.Approved); Assert.Equal("initial",approved.RollbackPolicy!.PolicyId);
    }
    [Fact] public void Analytics_ranking_is_deterministic_and_prediction_metrics_are_computed()
    {
        IXCoreAnalytics analytics=new XCoreAnalytics(); var routes=new[]{new CandidateRoute("b","reviewer","safe",[new("success_rate",.8)]),new CandidateRoute("a","reviewer","safe",[new("success_rate",.8)])};
        Assert.Equal(new[]{"a","b"},analytics.Rank(routes).Select(x=>x.RouteId)); var evaluation=analytics.EvaluatePredictions([1,2],[1,3],[.9,.2]); Assert.Equal(.5,evaluation.MeanAbsoluteError,6);
    }
    [Fact] public void Scoring_enforces_template_toolchain_closure_and_unique_bounded_features()
    {
        var service=Create(new XCore9Options(MaxFeaturesPerRun:2));
        Assert.Throws<UnauthorizedAccessException>(()=>service.ScoreRoutes([new("route","reviewer","other",[new("success_rate",.8)])]));
        Assert.Throws<InvalidOperationException>(()=>service.ScoreRoutes([new("route","reviewer","safe",[new("success_rate",.8),new("success_rate",.9)])]));
        Assert.Throws<InvalidOperationException>(()=>service.ScoreRoutes([new("route","reviewer","safe",[new("success_rate",double.PositiveInfinity)])]));
    }
    [Fact] public async Task Promotion_rejects_non_finite_metrics_and_snapshots_rules()
    {
        var rules=new Dictionary<string,string>{{"route","safe"}}; var service=Create();
        var invalid=await service.EvaluatePromotionAsync(new(new("invalid",2,rules,null),new(100,double.NaN,.2,.9,true),"guardian",[new("https://evidence.test/1")]),"guardian",default);
        Assert.False(invalid.Approved);
        var approved=await service.EvaluatePromotionAsync(new(new("candidate",2,rules,null),new(100,.4,.2,.9,true),"guardian",[new("https://evidence.test/1")]),"guardian",default);
        rules["route"]="mutated";
        Assert.Equal("safe",approved.ActivePolicy.Rules["route"]);
        Assert.Throws<NotSupportedException>(()=>((IDictionary<string,string>)approved.ActivePolicy.Rules)["route"]="mutated");
    }
    [Fact] public async Task Failed_audit_does_not_activate_candidate()
    {
        var service=Create(audit:new ThrowingAudit()); var request=new PromotionRequest(new("candidate",2,new Dictionary<string,string>(),null),new(100,.4,.2,.9,true),"guardian",[new("https://evidence.test/1")]);
        await Assert.ThrowsAsync<InvalidOperationException>(async()=>await service.EvaluatePromotionAsync(request,"guardian",default));
        var decision=await service.EvaluatePromotionAsync(request with { RequestedBy="xcore-9" },"guardian",default);
        Assert.Equal("initial",decision.ActivePolicy.PolicyId);
    }
    [Fact] public void Catalog_permissions_are_snapshotted_at_construction()
    {
        var allowed=new HashSet<string>{"safe"}; var tools=new HashSet<string>{"read","test"}; var dependencies=new HashSet<string>{"read"};
        var service=new XCore9Service(new XCoreAnalytics(),new Allow(),new Audit(),[new("reviewer",1,2,512,allowed,"sha256:fixed")],[new("safe",tools),new("late",new HashSet<string>{"late-tool"})],[new("read",new HashSet<string>()),new("test",dependencies),new("late-tool",new HashSet<string>())],new("initial",1,new Dictionary<string,string>(),null));
        allowed.Add("late"); tools.Add("late-tool"); dependencies.Add("late-tool");
        Assert.Throws<UnauthorizedAccessException>(()=>service.ScoreRoutes([new("route","reviewer","late",[new("success_rate",.8)])]));
        Assert.Equal(new[]{"read","test"},service.ConstructToolchain("safe").OrderedToolIds);
    }
    [Fact] public async Task Concurrent_promotions_are_audited_and_returned_in_policy_order()
    {
        var audit=new SequencedAudit(); var service=Create(audit:audit); var evidence=new[]{new Uri("https://evidence.test/1")};
        var first=service.EvaluatePromotionAsync(new(new("first",2,new Dictionary<string,string>(),null),new(100,.4,.2,.9,true),"guardian",evidence),"guardian",default).AsTask();
        await audit.FirstWriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second=service.EvaluatePromotionAsync(new(new("second",3,new Dictionary<string,string>(),null),new(100,.4,.2,.9,true),"guardian",evidence),"guardian",default).AsTask();
        Assert.False(second.IsCompleted); audit.ReleaseFirstWrite.SetResult();
        var decisions=await Task.WhenAll(first,second);
        Assert.Equal(new[]{"first","second"},audit.CorrelationIds);
        Assert.All(audit.EventTypes,eventType=>Assert.Equal("xcore9.policy.promotion-approved",eventType));
        Assert.Equal("initial",decisions[0].RollbackPolicy!.PolicyId);
        Assert.Equal("first",decisions[1].RollbackPolicy!.PolicyId);
        Assert.Equal("first",decisions[0].ActivePolicy.PolicyId);
        Assert.Equal("second",decisions[1].ActivePolicy.PolicyId);
    }
    private static XCore9Service Create(XCore9Options? options=null, IXCoreAuditSink? audit=null) => new(new XCoreAnalytics(),new Allow(),audit ?? new Audit(),[new("reviewer",1,2,512,new HashSet<string>{"safe"},"sha256:fixed")],[new("safe",new HashSet<string>{"read","test"}),new("other",new HashSet<string>{"read"})],[new("read",new HashSet<string>()),new("test",new HashSet<string>{"read"})],new("initial",1,new Dictionary<string,string>(),null),options);
    private sealed class Allow:IXCoreAuthorization { public ValueTask<bool> AuthorizeAsync(string actor,string capability,CancellationToken token)=>ValueTask.FromResult(true); }
    private sealed class Audit:IXCoreAuditSink { public ValueTask WriteAsync(string correlationId,string eventType,string actor,IReadOnlyList<Uri> links,CancellationToken token)=>ValueTask.CompletedTask; }
    private sealed class ThrowingAudit:IXCoreAuditSink { public ValueTask WriteAsync(string correlationId,string eventType,string actor,IReadOnlyList<Uri> links,CancellationToken token)=>throw new InvalidOperationException("audit unavailable"); }
    private sealed class SequencedAudit:IXCoreAuditSink
    {
        public TaskCompletionSource FirstWriteStarted { get; }=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseFirstWrite { get; }=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<string> CorrelationIds { get; }=[];
        public List<string> EventTypes { get; }=[];
        public async ValueTask WriteAsync(string correlationId,string eventType,string actor,IReadOnlyList<Uri> links,CancellationToken token)
        {
            CorrelationIds.Add(correlationId.Replace("policy-",string.Empty,StringComparison.Ordinal)); EventTypes.Add(eventType);
            if(CorrelationIds.Count==1) { FirstWriteStarted.SetResult(); await ReleaseFirstWrite.Task.WaitAsync(token); }
        }
    }
}
