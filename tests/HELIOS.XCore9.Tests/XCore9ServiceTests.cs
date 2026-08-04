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
    private static XCore9Service Create(XCore9Options? options=null) => new(new XCoreAnalytics(),new Allow(),new Audit(),[new("reviewer",1,2,512,new HashSet<string>{"safe"},"sha256:fixed")],[new("safe",new HashSet<string>{"read","test"})],[new("read",new HashSet<string>()),new("test",new HashSet<string>{"read"})],new("initial",1,new Dictionary<string,string>(),null),options);
    private sealed class Allow:IXCoreAuthorization { public ValueTask<bool> AuthorizeAsync(string actor,string capability,CancellationToken token)=>ValueTask.FromResult(true); }
    private sealed class Audit:IXCoreAuditSink { public ValueTask WriteAsync(string correlationId,string eventType,string actor,IReadOnlyList<Uri> links,CancellationToken token)=>ValueTask.CompletedTask; }
}
