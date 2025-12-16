private async void LiftPanelDetailsControl_Closed(object sender, WindowEventArgs args)
{
    // 🔹 If logout is in progress → close immediately, no dialog
    if (LandingScreen.IsLogoutInProgress)
    {
        CleanupBeforeClose();
        return;
    }

    // 🔹 If this window is already confirmed to close, allow it
    if (_isClosingConfirmed)
        return;

    // 🔹 Prevent window from closing until user decides
    args.Handled = true;

    // 🔹 If your own confirmation dialog is already open → do not open again
    if (_activeDialog != null)
        return;

    // 🔥 VERY IMPORTANT:
    // FORCE CLOSE ANY OTHER OPEN DIALOG (PasswordDialog, Control Dialog, etc.)
    try
    {
        PasswordDialog?.Hide();   // If open, CLOSE it safely
    }
    catch { }

    bool hasBlinkingButtons = _blinkingButtons.Count > 0;
    int blinkingCount = _blinkingButtons.Count;

    string contentMessage;

    if (hasBlinkingButtons)
    {
        contentMessage =
            $"⚠️ Warning: {blinkingCount} control button{(blinkingCount > 1 ? "s are" : " is")} currently active.\n\n" +
            "You must deactivate ALL blinking control buttons before closing this window.\n\n" +
            "Do you want to close anyway?";
    }
    else
    {
        contentMessage =
            $"Do you want to close the details window for {_panel?.Name ?? "this lift panel"}?";
    }

    // 🔹 Create confirmation dialog
    _activeDialog = new ContentDialog()
    {
        Title = hasBlinkingButtons
            ? "Cannot Close - Active Controls Detected"
            : "Close Lift Panel Details",

        Content = contentMessage,
        PrimaryButtonText = hasBlinkingButtons ? "OK (Cannot Close)" : "Yes",
        CloseButtonText = hasBlinkingButtons ? "Cancel" : "No",
        DefaultButton = ContentDialogButton.Close,
        XamlRoot = this.Content.XamlRoot,
        IsPrimaryButtonEnabled = !hasBlinkingButtons
    };

    // 🔹 SAFE dialog show (no crash)
    var result = await _activeDialog.ShowAsync();

    // Reset activeDialog so next close attempt can open a new one
    _activeDialog = null;

    // If blinking buttons exist → just exit (user must stop them first)
    if (hasBlinkingButtons)
        return;

    // User clicked "Yes"
    if (result == ContentDialogResult.Primary)
    {
        _isClosingConfirmed = true;
        CleanupBeforeClose();
        this.Close();
    }

    // If "No" or "Cancel" → do nothing
}
