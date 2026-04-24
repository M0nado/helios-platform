using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HELIOS.Platform.Phase10.BuilderUI
{
    /// <summary>
    /// Multi-step wizard engine implementing the 7-step builder process.
    /// Handles navigation, validation, and state management.
    /// </summary>
    public class StepWizardEngine
    {
        private readonly IBuilderUIService _service;
        private int _currentStepNumber;
        private List<WizardStep> _steps;
        private Dictionary<int, Func<Task<List<string>>>> _stepValidators;

        private const int TOTAL_STEPS = 7;
        private const int WELCOME_STEP = 1;
        private const int TARGET_STEP = 2;
        private const int VERSION_STEP = 3;
        private const int PACKAGES_STEP = 4;
        private const int PROFILE_STEP = 5;
        private const int CONFIG_STEP = 6;
        private const int REVIEW_STEP = 7;

        public int CurrentStep => _currentStepNumber;
        public bool CanGoBack => _currentStepNumber > WELCOME_STEP;
        public bool CanGoForward => _currentStepNumber < REVIEW_STEP;

        public StepWizardEngine(IBuilderUIService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _currentStepNumber = WELCOME_STEP;
            _steps = new List<WizardStep>();
            _stepValidators = new Dictionary<int, Func<Task<List<string>>>>();
        }

        /// <summary>
        /// Initialize the wizard engine with all steps and validators.
        /// </summary>
        public async Task InitializeAsync()
        {
            await InitializeStepsAsync();
            InitializeValidators();
        }

        /// <summary>
        /// Initialize all 7 wizard steps.
        /// </summary>
        private async Task InitializeStepsAsync()
        {
            _steps = new List<WizardStep>
            {
                // Step 1: Welcome & System Check
                new WizardStep
                {
                    StepNumber = WELCOME_STEP,
                    Title = "Welcome to HELIOS USB Builder",
                    Description = "Prepare your system for HELIOS installation",
                    Content = "This wizard will guide you through creating a bootable HELIOS USB drive. You'll select your target drive, choose Windows version, select packages, and customize your installation.",
                    CanGoBack = false,
                    CanGoForward = true,
                    IsValid = true
                },

                // Step 2: Select Target (USB/Disk)
                new WizardStep
                {
                    StepNumber = TARGET_STEP,
                    Title = "Select Target Drive",
                    Description = "Choose USB or disk for installation",
                    Content = "Select the drive where HELIOS will be installed. Ensure you have a drive with sufficient free space.",
                    CanGoBack = true,
                    CanGoForward = false,
                    IsValid = false
                },

                // Step 3: Choose Windows Version
                new WizardStep
                {
                    StepNumber = VERSION_STEP,
                    Title = "Select Windows Version",
                    Description = "Choose Home, Pro, or Enterprise edition",
                    Content = "Select the Windows edition that best fits your needs.",
                    CanGoBack = true,
                    CanGoForward = false,
                    IsValid = false
                },

                // Step 4: Select Packages
                new WizardStep
                {
                    StepNumber = PACKAGES_STEP,
                    Title = "Select Packages",
                    Description = "Choose system and application packages",
                    Content = "Select which packages to include in your installation. Packages are organized by category.",
                    CanGoBack = true,
                    CanGoForward = true,
                    IsValid = true
                },

                // Step 5: Choose Profile
                new WizardStep
                {
                    StepNumber = PROFILE_STEP,
                    Title = "Choose Optimization Profile",
                    Description = "Select performance optimization profile",
                    Content = "Choose a profile that matches your use case: Gaming, Development, Workstation, or Balanced.",
                    CanGoBack = true,
                    CanGoForward = false,
                    IsValid = false
                },

                // Step 6: Configure HELIOS
                new WizardStep
                {
                    StepNumber = CONFIG_STEP,
                    Title = "Configure HELIOS",
                    Description = "Fine-tune HELIOS settings",
                    Content = "Configure advanced settings for your HELIOS installation.",
                    CanGoBack = true,
                    CanGoForward = true,
                    IsValid = true
                },

                // Step 7: Review & Create
                new WizardStep
                {
                    StepNumber = REVIEW_STEP,
                    Title = "Review & Create",
                    Description = "Verify your selections and start deployment",
                    Content = "Review all your selections below. Click 'Deploy' to begin creating your HELIOS USB drive.",
                    CanGoBack = true,
                    CanGoForward = false,
                    IsValid = false
                }
            };

            await Task.CompletedTask;
        }

        /// <summary>
        /// Initialize step-specific validators.
        /// </summary>
        private void InitializeValidators()
        {
            _stepValidators[TARGET_STEP] = ValidateTargetStepAsync;
            _stepValidators[VERSION_STEP] = ValidateVersionStepAsync;
            _stepValidators[PROFILE_STEP] = ValidateProfileStepAsync;
            _stepValidators[REVIEW_STEP] = ValidateReviewStepAsync;
        }

        /// <summary>
        /// Get current step information.
        /// </summary>
        public async Task<WizardStep> GetCurrentStepAsync()
        {
            var step = _steps.FirstOrDefault(s => s.StepNumber == _currentStepNumber);
            return await Task.FromResult(step);
        }

        /// <summary>
        /// Navigate to next step.
        /// </summary>
        public async Task<bool> GoToNextStepAsync()
        {
            if (_currentStepNumber >= REVIEW_STEP)
                return false;

            // Validate current step before proceeding
            var errors = await ValidateCurrentStepAsync();
            if (errors.Any())
            {
                return false;
            }

            _currentStepNumber++;
            return true;
        }

        /// <summary>
        /// Navigate to previous step.
        /// </summary>
        public async Task<bool> GoToPreviousStepAsync()
        {
            if (_currentStepNumber <= WELCOME_STEP)
                return false;

            _currentStepNumber--;
            return true;
        }

        /// <summary>
        /// Go to specific step.
        /// </summary>
        public async Task<bool> GoToStepAsync(int stepNumber)
        {
            if (stepNumber < WELCOME_STEP || stepNumber > REVIEW_STEP)
                return false;

            _currentStepNumber = stepNumber;
            return await Task.FromResult(true);
        }

        /// <summary>
        /// Validate current step.
        /// </summary>
        public async Task<List<string>> ValidateCurrentStepAsync()
        {
            if (_stepValidators.ContainsKey(_currentStepNumber))
            {
                return await _stepValidators[_currentStepNumber]();
            }

            return new List<string>();
        }

        /// <summary>
        /// Get all wizard steps.
        /// </summary>
        public async Task<List<WizardStep>> GetAllStepsAsync()
        {
            return await Task.FromResult(_steps.ToList());
        }

        /// <summary>
        /// Validate target drive selection.
        /// </summary>
        private async Task<List<string>> ValidateTargetStepAsync()
        {
            var errors = new List<string>();
            var drives = await _service.GetAvailableDrivesAsync();

            if (!drives.Any())
            {
                errors.Add("No drives available for selection.");
            }

            return errors;
        }

        /// <summary>
        /// Validate Windows version selection.
        /// </summary>
        private async Task<List<string>> ValidateVersionStepAsync()
        {
            var errors = new List<string>();
            var selectedVersion = await _service.GetSelectedWindowsVersionAsync();

            if (string.IsNullOrEmpty(selectedVersion))
            {
                errors.Add("Windows version must be selected.");
            }

            return errors;
        }

        /// <summary>
        /// Validate profile selection.
        /// </summary>
        private async Task<List<string>> ValidateProfileStepAsync()
        {
            var errors = new List<string>();
            var selectedProfile = await _service.GetSelectedProfileAsync();

            if (selectedProfile == null)
            {
                errors.Add("Optimization profile must be selected.");
            }

            return errors;
        }

        /// <summary>
        /// Validate final review step.
        /// </summary>
        private async Task<List<string>> ValidateReviewStepAsync()
        {
            var errors = new List<string>();
            var summary = await _service.GetDeploymentSummaryAsync();

            if (string.IsNullOrEmpty(summary?.TargetDrive))
                errors.Add("Target drive not selected.");

            if (string.IsNullOrEmpty(summary?.WindowsVersion))
                errors.Add("Windows version not selected.");

            if (!summary?.TermsAccepted ?? true)
                errors.Add("Terms and conditions must be accepted.");

            return errors;
        }
    }
}
