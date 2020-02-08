using UnityEngine;
using System.Threading;
using Nescafe;

public class Emulator : MonoBehaviour {

    public TextAsset ROMFile;
    public Texture2D emulatorDisplay;

    private Thread emuUpdate;

    private Console console;
    private Color[] palette;

    void Start(){
        console = new Console();

        InitPalette();
        LoadCartridge();
    }

    void OnDisable(){
        StopConsole();
    }

    void Update(){
        if(console.DrawReady){
            // https://docs.unity3d.com/ScriptReference/Texture2D.SetPixels.html maybe

            for(int i = 0; i < 256 * 240; ++i){
                int x = i % 256;
                int y = i / 256;

                Color color = palette[console.Ppu.BitmapData[i]];
                emulatorDisplay.SetPixel(x, y, color);
            }

            emulatorDisplay.Apply();
            console.DrawReady = false;
            console.Cont = true;
        } else {
            // Debug.Log("Skipped frame :|");
        }

        // Input
        console.Controller.setButtonState(Controller.Button.A, Input.GetKey(KeyCode.Z));
        console.Controller.setButtonState(Controller.Button.B, Input.GetKey(KeyCode.X));
        console.Controller.setButtonState(Controller.Button.Left, Input.GetKey(KeyCode.LeftArrow));
        console.Controller.setButtonState(Controller.Button.Right, Input.GetKey(KeyCode.RightArrow));
        console.Controller.setButtonState(Controller.Button.Up, Input.GetKey(KeyCode.UpArrow));
        console.Controller.setButtonState(Controller.Button.Down, Input.GetKey(KeyCode.DownArrow));
        console.Controller.setButtonState(Controller.Button.Start, Input.GetKey(KeyCode.Return));
        console.Controller.setButtonState(Controller.Button.Select, Input.GetKey(KeyCode.Backspace));
    }

    private Color ColorUtil(int r, int g, int b){
        return new Color(
            (float) r / (float)255,
            (float) g / (float)255,
            (float) b / (float)255,
            1.0f
        );
    }

    // Non unity stuff below here
    void InitPalette(){
        palette = new Color[0x40];

        palette[0x0] = ColorUtil(84, 84, 84);
        palette[0x1] = ColorUtil(0, 30, 116);
        palette[0x2] = ColorUtil(8, 16, 144);
        palette[0x3] = ColorUtil(48, 0, 136);
        palette[0x4] = ColorUtil(68, 0, 100);
        palette[0x5] = ColorUtil(92, 0, 48);
        palette[0x6] = ColorUtil(84, 4, 0);
        palette[0x7] = ColorUtil(60, 24, 0);
        palette[0x8] = ColorUtil(32, 42, 0);
        palette[0x9] = ColorUtil(8, 58, 0);
        palette[0xa] = ColorUtil(0, 64, 0);
        palette[0xb] = ColorUtil(0, 60, 0);
        palette[0xc] = ColorUtil(0, 50, 60);
        palette[0xd] = ColorUtil(0, 0, 0);
        palette[0xe] = ColorUtil(0, 0, 0);
        palette[0xf] = ColorUtil(0, 0, 0);
        palette[0x10] = ColorUtil(152, 150, 152);
        palette[0x11] = ColorUtil(8, 76, 196);
        palette[0x12] = ColorUtil(48, 50, 236);
        palette[0x13] = ColorUtil(92, 30, 228);
        palette[0x14] = ColorUtil(136, 20, 176);
        palette[0x15] = ColorUtil(160, 20, 100);
        palette[0x16] = ColorUtil(152, 34, 32);
        palette[0x17] = ColorUtil(120, 60, 0);
        palette[0x18] = ColorUtil(84, 90, 0);
        palette[0x19] = ColorUtil(40, 114, 0);
        palette[0x1a] = ColorUtil(8, 124, 0);
        palette[0x1b] = ColorUtil(0, 118, 40);
        palette[0x1c] = ColorUtil(0, 102, 120);
        palette[0x1d] = ColorUtil(0, 0, 0);
        palette[0x1e] = ColorUtil(0, 0, 0);
        palette[0x1f] = ColorUtil(0, 0, 0);
        palette[0x20] = ColorUtil(236, 238, 236);
        palette[0x21] = ColorUtil(76, 154, 236);
        palette[0x22] = ColorUtil(120, 124, 236);
        palette[0x23] = ColorUtil(176, 98, 236);
        palette[0x24] = ColorUtil(228, 84, 236);
        palette[0x25] = ColorUtil(236, 88, 180);
        palette[0x26] = ColorUtil(236, 106, 100);
        palette[0x27] = ColorUtil(212, 136, 32);
        palette[0x28] = ColorUtil(160, 170, 0);
        palette[0x29] = ColorUtil(116, 196, 0);
        palette[0x2a] = ColorUtil(76, 208, 32);
        palette[0x2b] = ColorUtil(56, 204, 108);
        palette[0x2c] = ColorUtil(56, 180, 204);
        palette[0x2d] = ColorUtil(60, 60, 60);
        palette[0x2e] = ColorUtil(0, 0, 0);
        palette[0x2f] = ColorUtil(0, 0, 0);
        palette[0x30] = ColorUtil(236, 238, 236);
        palette[0x31] = ColorUtil(168, 204, 236);
        palette[0x32] = ColorUtil(188, 188, 236);
        palette[0x33] = ColorUtil(212, 178, 236);
        palette[0x34] = ColorUtil(236, 174, 236);
        palette[0x35] = ColorUtil(236, 174, 212);
        palette[0x36] = ColorUtil(236, 180, 176);
        palette[0x37] = ColorUtil(228, 196, 144);
        palette[0x38] = ColorUtil(204, 210, 120);
        palette[0x39] = ColorUtil(180, 222, 120);
        palette[0x3a] = ColorUtil(168, 226, 144);
        palette[0x3b] = ColorUtil(152, 226, 180);
        palette[0x3c] = ColorUtil(160, 214, 228);
        palette[0x3d] = ColorUtil(160, 162, 160);
        palette[0x3e] = ColorUtil(0, 0, 0);
        palette[0x3f] = ColorUtil(0, 0, 0);
    }

    void LoadCartridge(){
        if(console.LoadCartridge(ROMFile.bytes)){
            StartConsole();
        } else {
            Debug.LogError("Error loading cartridge");
        }
    }

    void StopConsole(){
        console.Cont = false;
        console.Stop = true;

        emuUpdate.Join();
    }

    void StartConsole(){
        console.Cont = true;
        console.Stop = false;

        emuUpdate = new Thread(console.Start);
        emuUpdate.Start();
    }

/*
    // Input
    void OnKeyDown(object sender, KeyEventArgs e)
    {
        SetControllerButton(true, e);
    }

    void OnKeyUp(object sender, KeyEventArgs e)
    {
        SetControllerButton(false, e);
    }

    void SetControllerButton(bool state, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Z:
                _console.Controller.setButtonState(Controller.Button.A, state);
                break;
            case Keys.X:
                _console.Controller.setButtonState(Controller.Button.B, state);
                break;
            case Keys.Left:
                _console.Controller.setButtonState(Controller.Button.Left, state);
                break;
            case Keys.Right:
                _console.Controller.setButtonState(Controller.Button.Right, state);
                break;
            case Keys.Up:
                _console.Controller.setButtonState(Controller.Button.Up, state);
                break;
            case Keys.Down:
                _console.Controller.setButtonState(Controller.Button.Down, state);
                break;
            case Keys.Q:
                _console.Controller.setButtonState(Controller.Button.Start, state);
                break;
            case Keys.W:
                _console.Controller.setButtonState(Controller.Button.Select, state);
                break;
        }
    }
}*/

}
