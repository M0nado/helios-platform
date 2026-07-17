const $ = id => document.getElementById(id);

document.querySelectorAll('.tab').forEach(button => button.addEventListener('click', () => {
  document.querySelectorAll('.tab,.panel').forEach(element => element.classList.remove('active'));
  button.classList.add('active');
  $(button.dataset.panel).classList.add('active');
}));

async function post(url, data, status) {
  $(status).textContent = 'Working…';
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data)
  });
  const result = await response.json();
  if (!response.ok) throw new Error(result.error || `Request failed (${response.status})`);
  $(status).textContent = 'Ready for review';
  return result;
}

$('generate').addEventListener('click', async () => {
  try {
    const result = await post('/setup/bootstrap', {
      tenantId: $('tenant').value,
      subscriptionId: $('subscription').value,
      resourceGroup: $('group').value,
      environment: $('environment').value
    }, 'setupStatus');
    $('script').textContent = result.script;
    const roles = Object.keys(result.shellPackets).join(', ');
    $('digest').textContent = `SHA-256: ${result.scriptSha256} · ${result.subscriptionSelection} · roles: ${roles} · plan only · no secrets`;
  } catch (error) {
    $('setupStatus').textContent = error.message;
  }
});

$('copy').addEventListener('click', () => navigator.clipboard.writeText($('script').textContent));

$('propose').addEventListener('click', async () => {
  try {
    const result = await post('/upgrades/propose', {
      capability: $('capability').value,
      reason: $('reason').value,
      target: 'helios-control'
    }, 'upgradeStatus');
    $('proposal').textContent = JSON.stringify(result, null, 2);
  } catch (error) {
    $('upgradeStatus').textContent = error.message;
  }
});
