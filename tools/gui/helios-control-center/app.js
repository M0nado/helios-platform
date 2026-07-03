const sections=[
 {id:'setup',title:'Deep Setup',buttons:[['Deep Setup All','./tools/helios.ps1 setup deep-all'],['Final Gate','./tools/helios.ps1 gate final'],['Autoconnect','./tools/helios.ps1 connect verify']]},
 {id:'github',title:'GitHub Takeover',buttons:[['Takeover Status','./tools/helios.ps1 github takeover'],['Mass Score','./tools/helios.ps1 github mass-score'],['Conflict Forecast','./tools/helios.ps1 github conflict-forecast'],['Priority Branches','python3 scripts/github/conflict_forecast.py']]},
 {id:'agents',title:'JRPG Agent Party',buttons:[['Agent Party','./tools/helios.ps1 agents party'],['Fleet Deploy','./tools/helios.ps1 fleet deploy'],['Agent XP','./tools/helios.ps1 agents xp'],['Runtime Matrix','./tools/helios.ps1 agents runtime']]},
 {id:'fix',title:'Code Fix Center',buttons:[['Fix Center','./tools/helios.ps1 fix center'],['C# Center','./tools/helios.ps1 fix csharp'],['F# Report','./tools/helios.ps1 test fsharp-report'],['C++ Benchmark','./tools/helios.ps1 test native-benchmark'],['Python AIHub','./tools/helios.ps1 test python-aihub']]},
 {id:'azure',title:'Azure & Vault',buttons:[['Bicep Build','./tools/helios.ps1 azure report-build'],['Validate','./tools/helios.ps1 azure report-validate'],['What If','./tools/helios.ps1 azure report-what-if'],['Vault','./tools/helios.ps1 security vault']]},
 {id:'models',title:'Models & Copilot',buttons:[['Model Store','./tools/helios.ps1 models list'],['Provider Health','python3 scripts/automation/provider_health.py'],['Cost Optimizer','python3 scripts/automation/model_cost_speed_optimizer.py'],['Hermes/XCore','./tools/helios.ps1 hermes models'],['Copilot/M365','./tools/helios.ps1 m365 copilot']]},
 {id:'learning',title:'Learning & Audit',buttons:[['Core Learning','./tools/helios.ps1 learning core'],['Learning Summary','python3 scripts/learning/summarize_learning.py'],['Audit Latest','./tools/helios.ps1 audit latest'],['Policy Check','./tools/helios.ps1 policy check']]}
];
const reports=['reports/final-gate/final-gate.json','reports/mass-integration/conflict-forecast.json','reports/mass-integration/priority-branches.json','reports/agents/agent-party.json','reports/fleet/fleet-deploy.json','reports/learning/core-ai-learning.json','reports/learning/summary.json','reports/code-fix-center/code-fix-center.json','reports/azure/what-if.json','reports/ai-providers/provider-health.json','reports/ai-providers/cost-speed-optimizer.json','reports/policy/policy-gate.json','reports/audit/latest.md'];
const party=[
 {slot:'Leader',name:'csharp-core',job:'C# Vanguard',xp:60,abilities:['Compile Break','Namespace Mend','Contract Guard'],cmd:'./tools/helios.ps1 fix csharp'},
 {slot:'Guardian',name:'azure-infra',job:'Azure Oracle',xp:70,abilities:['What-If Sight','Vault Seal','Deploy Ward'],cmd:'./tools/helios.ps1 azure report-what-if'},
 {slot:'Scout',name:'mass-integration',job:'Merge Ronin',xp:80,abilities:['Safe Merge','Conflict Sense','Auto-PR'],cmd:'./tools/helios.ps1 github conflict-forecast'},
 {slot:'Sage',name:'hermes-xcore',job:'XCore Sage',xp:55,abilities:['Model Swap','Token Aura','Local Fallback'],cmd:'./tools/helios.ps1 hermes models'}
];
function copy(c){navigator.clipboard.writeText(c)}
function button([label,cmd]){return `<button onclick="copy('${cmd.replaceAll("'","\\'")}')">${label}<small>${cmd}</small></button>`}
const tabs=document.getElementById('tabs'); const app=document.getElementById('app');
tabs.innerHTML=sections.map(s=>`<a href="#${s.id}">${s.title}</a>`).join('');
app.innerHTML=`<section class="hero"><h2>Fleet XP ${party.reduce((a,p)=>a+p.xp,0)}</h2><p>All buttons copy safe commands. Enable the runner bridge only after policy and audit gates are configured.</p></section>
<section class="grid">${party.map(p=>`<div class="card party"><h2>${p.slot}: ${p.name}</h2><h3>${p.job} · Lv ${Math.max(1,Math.floor(p.xp/25))}</h3><div class="bar"><span style="width:${Math.min(100,p.xp)}%"></span></div><p>${p.abilities.map(a=>`<b class="ability">${a}</b>`).join(' ')}</p>${button(['Copy ability command',p.cmd])}</div>`).join('')}</section>
${sections.map(s=>`<section id="${s.id}"><h2>${s.title}</h2><div class="grid">${s.buttons.map(button).map(b=>`<div class="card shop">${b}</div>`).join('')}</div></section>`).join('')}
<section><h2>Report Sources</h2><div class="grid">${reports.map(r=>`<div class="card"><h3>${r}</h3><pre>${r}</pre></div>`).join('')}</div></section>`;
