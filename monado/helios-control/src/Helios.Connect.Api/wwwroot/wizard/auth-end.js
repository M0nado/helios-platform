(async () => {
  const status = document.getElementById('authStatus');
  try {
    if (!window.microsoftTeams?.app?.initialize || !window.microsoftTeams?.authentication?.notifySuccess) {
      throw new Error('Microsoft host authentication is unavailable.');
    }
    await window.microsoftTeams.app.initialize();
    status.textContent = 'Microsoft sign-in complete. This window can close.';
    // Return only a constant status marker; access tokens never cross the popup boundary.
    window.microsoftTeams.authentication.notifySuccess('session-established');
  } catch {
    status.textContent = 'Microsoft sign-in could not be completed. Close this window and try again.';
    if (window.microsoftTeams?.authentication?.notifyFailure) {
      window.microsoftTeams.authentication.notifyFailure('authentication-incomplete');
    }
  }
})();
