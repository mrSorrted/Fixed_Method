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

        j.[key]       AS ElementNo,

        CAST(j.value AS INT) AS ElementValue

    FROM vw_eledata d

    CROSS APPLY OPENJSON(d.SIGNAL) j

),

Pivoted AS

(

    SELECT

        DeviceName,

        CurrentTimestamp,
 
        -- Current Floor (processed[0])

        MAX(CASE WHEN ElementNo = 0 THEN

            CASE 

                WHEN ElementValue > 128 THEN ElementValue & 0x7F

                ELSE ElementValue

            END

        END) AS CurrentFloor,
 
        -- Status (processed[3])

        MAX(CASE WHEN ElementNo = 3 THEN

            CASE 

                WHEN (CASE WHEN ElementValue > 128 THEN ElementValue & 0x7F ELSE ElementValue END) BETWEEN 1 AND 14

                    THEN CHOOSE(

                        CASE WHEN ElementValue > 128 THEN ElementValue & 0x7F ELSE ElementValue END,

                        'Overload','Seismic','Attendant','Maintenance','Emergency','FIRE','Alarm','VOIP Call','ARD','Out of Order','Overload','Low Battery','Power Failure','Power Failed'

                    )

                ELSE CAST(ElementValue AS VARCHAR(10))

            END

        END) AS Status,
 
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

    Status,

    CurrentFloor,

    DoorStatus,

    Arrow,

    CASE 

        WHEN Arrow LIKE '%Up Direction Run%'

          OR Arrow LIKE '%Down Direction Run%'

        THEN 15

        ELSE 0

    END AS RunTime,

    CurrentTimestamp

FROM Pivoted;
 
 
 
select * from vw_DeviceDashboard
 
