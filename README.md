# ClipboardToPNG

This is a tiny helper utility for fixing an annoyance I have with Photoshop on Windows - the fact that it does not handle transparency. 

What this does is save the image as a .png file (located at `%TEMP%\clip.png`), and returns `0` if that successfully happened, and `1` if it didn't.

Using that, you can, for example, use AutoHotKey to intercept `Ctrl`+`V` presses when Photoshop is open, and then if ClipboardToPNG returned 0, run a Photoshop action that would paste the temporary file, otherwise, perform a standard paste:

```ahk
DoPhotoshopPaste() {
    RunWait, %A_ScriptDir%\ClipboardToPNG.exe ; run utility, wait for it to complete
    if (ErrorLevel == 0) { ; if error code is 0
        SendEvent, +^{F12} ; press Shift+Ctrl+F12 to run the designated Photoshop action to paste
    }
    else { 
        SendEvent, ^v ; else, just perform a standard paste.
    }
}

#IfWinActive ahk_exe Photoshop.exe ; only activate this hotkey when photoshop is active
    ^v::DoPhotoshopPaste()
#IfWinActive
```
