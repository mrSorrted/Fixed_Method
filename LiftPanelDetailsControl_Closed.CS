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


SQL Query Start


CREATE OR ALTER VIEW vw_DeviceDashboard

AS

WITH Parsed AS

(

    SELECT

        d.DeviceName,

        d.CurrentTimestamp,

        CAST(d.Status AS INT) AS BaseStatus,

        j.[key] AS ElementNo,

        CAST(j.value AS INT) AS ElementValue

    FROM vw_eledata d

    OUTER APPLY OPENJSON(d.SIGNAL) j  -- include empty SIGNAL

),

Pivoted AS

(

    SELECT

        DeviceName,

        CurrentTimestamp,

        MAX(BaseStatus) AS BaseStatus,
 
        -- Current Floor (processed[0])

        MAX(CASE WHEN ElementNo = 0 THEN

            CASE 

                WHEN ElementValue > 128 THEN ElementValue & 0x7F

                ELSE ElementValue

            END

        END) AS CurrentFloor,
 
        -- Status from processed[3] if not offline

        MAX(CASE WHEN ElementNo = 3 THEN

            CASE 

                WHEN (CASE WHEN ElementValue > 128 THEN ElementValue & 0x7F ELSE ElementValue END) = 0

                    THEN 'Normal Operation'

                WHEN (CASE WHEN ElementValue > 128 THEN ElementValue & 0x7F ELSE ElementValue END) BETWEEN 1 AND 14

                    THEN CHOOSE(

                        CASE WHEN ElementValue > 128 THEN ElementValue & 0x7F ELSE ElementValue END,

                        'Overload','Seismic','Attendant','Maintenance','Emergency','FIRE','Alarm','VOIP Call','ARD','Out of Order','Overload','Low Battery','Power Failure','Power Failed'

                    )

                ELSE CAST(ElementValue AS VARCHAR(10))

            END

        END) AS StatusFromSignal,
 
        -- Door Status (processed[13])

        MAX(CASE WHEN ElementNo = 13 THEN

            CASE 

                WHEN (ElementValue & 0xC0) >= 128 THEN 'Door Open'

                ELSE 'Door Close'

            END

        END) AS DoorStatus,
 
        -- Arrow (processed[1])

        MAX(CASE WHEN ElementNo = 1 THEN

            LTRIM(

                CONCAT(

                    CASE WHEN (ElementValue & 64) = 64 THEN 'Down Arrow Stable;' ELSE '' END,

                    CASE WHEN (ElementValue & 32) = 32 THEN 'Up Arrow Stable;' ELSE '' END,

                    CASE WHEN (ElementValue & 16) = 16 THEN 'Power Save;' ELSE '' END,

                    CASE 

                        WHEN (ElementValue & 3) = 0 THEN 'No Arrow'

                        ELSE

                            CONCAT(

                                CASE WHEN (ElementValue & 2) = 2 THEN 'Down Direction Run;' ELSE '' END,

                                CASE WHEN (ElementValue & 1) = 1 THEN 'Up Direction Run' ELSE '' END

                            )

                    END

                )

            )

        END) AS Arrow
 
    FROM Parsed

    GROUP BY DeviceName, CurrentTimestamp

)

SELECT

    DeviceName,

    -- If BaseStatus = 1 → Offline, else processed[3], blank if NULL

    CASE 

        WHEN BaseStatus = 1 THEN 'Offline'

        ELSE ISNULL(StatusFromSignal, '')

    END AS Status,

    ISNULL(CAST(CurrentFloor AS VARCHAR(10)), '') AS CurrentFloor,

    ISNULL(DoorStatus, '') AS DoorStatus,

    ISNULL(Arrow, '') AS Arrow,

    CASE 

        WHEN Arrow LIKE '%Up Direction Run%'

          OR Arrow LIKE '%Down Direction Run%'

        THEN 7

        ELSE 0

    END AS RunTime,

    CurrentTimestamp

FROM Pivoted;
 
 
select * from vw_DeviceDashboard














        private async void UserProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserSession.CurrentUser == null)
                return;

            // 🔥 If another dialog is open, CLOSE IT FIRST
            try
            {
                _activeDialog?.Hide();
            }
            catch { }

            _activeDialog = null;

            var dialog = new ContentDialog
            {
                Title = "User Profile",
                Content =
                    $"Username: {UserSession.CurrentUser.Username}\n" +
                    $"Role: {UserSession.CurrentUser.Role}\n" +
                    $"Email: {UserSession.CurrentUser.Email}\n" +
                    $"Phone: {UserSession.CurrentUser.PhoneNumber}\n" +
                    $"Login Time: {_loginTime:MMM dd, yyyy HH:mm:ss}",
                PrimaryButtonText = "Logout",
                CloseButtonText = "Close",
                XamlRoot = this.Content.XamlRoot
            };

            _activeDialog = dialog;

            var result = await dialog.ShowAsync();

            _activeDialog = null;

            if (result == ContentDialogResult.Primary)
            {
                await PerformLogout("Profile Dialog");
            }
        }


        private async void ShowAboutDialog()
        {
            // 🔥 Replace any existing dialog
            try
            {
                _activeDialog?.Hide();
            }
            catch { }

            _activeDialog = null;

            var userInfo = UserSession.CurrentUser != null
                ? $"\n\nCurrent User: {UserSession.CurrentUser.Username} ({UserSession.CurrentUser.Role})\nSession Started: {_loginTime:MMM dd, yyyy HH:mm:ss}"
                : "";

            var dialog = new ContentDialog
            {
                Title = "About VHT CMS System",
                Content =
                    $"VHT Elevator Management System v2.0.0\n\n" +
                    $"A comprehensive solution for monitoring and controlling elevator systems.{userInfo}",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            _activeDialog = dialog;

            await dialog.ShowAsync();

            _activeDialog = null;
        }



        // Handle window closing event
        private ContentDialog? _activeDialog = null;

        private async void LandingScreen_Closed(object sender, WindowEventArgs args)
        {
            // 🔹 If logout is already in progress → allow close
            if (IsLogoutInProgress)
                return;

            // 🔹 If close already confirmed → allow close
            if (_isClosingConfirmed)
                return;

            // 🔹 Stop automatic window close
            args.Handled = true;

            // 🔹 If a dialog is already open → do NOT open another
            if (_activeDialog != null)
                return;

            // 🔥 Close any already-open dialog (Profile / About / Error)
            try
            {
                _activeDialog?.Hide();
            }
            catch { }

            _activeDialog = null;

            bool hasBlinkingButtons = false;
            try
            {
                hasBlinkingButtons = BlinkingButtonManager.HasAnyBlinkingButtons();
            }
            catch { }

            string message = hasBlinkingButtons
                ? "⚠️ Warning: Active controls are ON.\n\nClosing will deactivate everything.\n\nDo you want to exit anyway?"
                : "Are you sure you want to exit the VHT CMS System?";

            // 🔹 Create exit confirmation dialog (NO SOUND)
            _activeDialog = new ContentDialog
            {
                Title = "Exit Application",
                Content = message,
                PrimaryButtonText = "Yes, Exit",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            // 🔹 Show dialog safely
            var result = await _activeDialog.ShowAsync();

            // 🔹 Reset dialog reference
            _activeDialog = null;

            // 🔹 User confirmed exit
            if (result == ContentDialogResult.Primary)
            {
                _isClosingConfirmed = true;
                UserSession.ClearSession();
                this.Close();
                Application.Current.Exit();
            }

            // Cancel → do nothing
        }

 
