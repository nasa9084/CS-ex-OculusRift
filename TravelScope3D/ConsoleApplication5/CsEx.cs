using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
//OculusVR
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpOVR;
//ovrVision
using ovrvision_app;

namespace CsEx
{
    using SharpDX.Toolkit;
    using SharpDX.Toolkit.Graphics;
    using SharpDX.DXGI;

    public class CsEx : Game
    {

        private bool SaveMode = false;

        private GraphicsDeviceManager graphicsDeviceManager;
        private HMD hmd;
        private COvrvision ovrcam = new COvrvision();

        private Texture2D warai, waraired;

        private System.Drawing.Bitmap cambmp;
        private Texture2D[] camTexture = new Texture2D[2];

  
        private SharpDX.Direct3D11.Texture2D mirrorTexture;

        private EyeRenderDesc[] eyeRenderDesc = new EyeRenderDesc[2];
        private PoseF[] eyeRenderPose = new PoseF[2];
        private SwapTexture[] eyeTexture = new SwapTexture[2];
        private LayerEyeFov layerEyeFov;
        private Vector3[] hmdToEyeViewOffset = new Vector3[2];

        private Vector3 headPos = new Vector3(0f, 0f, -5f);

        //ŽB‰e‚Ì‰ð‘œ“x
        //1/mag“x‚¸‚ÂŽB‰e
        private const int rollmag = 2;
        private const int yawmag = 2;
        private const int pitchmag = 1;


        private string posname = "default";
        private string filename;
        private int yaw, pitch, roll;
        private int rollrange = 3* rollmag;

        private const int upLimit=60, downLimit=-45, capDegree=1;
        private const int yawOffset = 180*yawmag ;
        private const int pitchOffset = 180*pitchmag;
        //private bool[,,] isStored = new bool[2,361/capDegree , 361/capDegree];
        private bool[, ,] isStored = new bool[2, 361 * yawmag / capDegree, 361 * pitchmag / capDegree];

        public CsEx(){
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            //Content.RootDirectory = "Content";
            OVR.Initialize();
            hmd = OVR.HmdCreate(0) ?? OVR.HmdCreateDebug(HMDType.DK2);
            hmd.ConfigureTracking(TrackingCapabilities.Orientation | TrackingCapabilities.Position | TrackingCapabilities.MagYawCorrection, TrackingCapabilities.None);
            hmd.EnabledCaps = HMDCapabilities.LowPersistence | HMDCapabilities.DynamicPrediction;

            hmdToEyeViewOffset[0] = hmd.GetRenderDesc(EyeType.Left, hmd.DefaultEyeFov[0]).HmdToEyeViewOffset;
            hmdToEyeViewOffset[1] = hmd.GetRenderDesc(EyeType.Right, hmd.DefaultEyeFov[1]).HmdToEyeViewOffset;

            // Match back buffer size with HMD resolution
            graphicsDeviceManager.PreferredBackBufferWidth = hmd.Resolution.Width;
            graphicsDeviceManager.PreferredBackBufferHeight = hmd.Resolution.Height;
            graphicsDeviceManager.PreferredFullScreenOutputIndex = 1;

            bool res = ovrcam.Open();
            Console.WriteLine("OVR OPEN:" + res.ToString());
            ovrcam.useProcessingQuality = 2;
        }

        protected SharpDX.Direct3D11.Device Device{
            get { return (SharpDX.Direct3D11.Device)GraphicsDevice; }
        }

        protected override void Initialize(){
            Window.Title = "CsEx";

            // Create our render target
            eyeTexture[0] = hmd.CreateSwapTexture(GraphicsDevice, Format.R8G8B8A8_UNorm, hmd.GetFovTextureSize(EyeType.Left, hmd.DefaultEyeFov[0]), true);
            eyeTexture[1] = hmd.CreateSwapTexture(GraphicsDevice, Format.R8G8B8A8_UNorm, hmd.GetFovTextureSize(EyeType.Right, hmd.DefaultEyeFov[1]), true);

            // Create our layer
            layerEyeFov = new LayerEyeFov
            {
                Header = new LayerHeader(LayerType.EyeFov, LayerFlags.None),
                ColorTextureLeft = eyeTexture[0].TextureSet,
                ColorTextureRight = eyeTexture[1].TextureSet,
                ViewportLeft = new Rect(0, 0, eyeTexture[0].Size.Width, eyeTexture[0].Size.Height),
                ViewportRight = new Rect(0, 0, eyeTexture[1].Size.Width, eyeTexture[1].Size.Height),
                FovLeft = hmd.DefaultEyeFov[0],
                FovRight = hmd.DefaultEyeFov[1]
            };

            // Keep eye view offsets
            eyeRenderDesc[0] = hmd.GetRenderDesc(EyeType.Left, hmd.DefaultEyeFov[0]);
            eyeRenderDesc[1] = hmd.GetRenderDesc(EyeType.Right, hmd.DefaultEyeFov[1]);
            hmdToEyeViewOffset[0] = eyeRenderDesc[0].HmdToEyeViewOffset;
            hmdToEyeViewOffset[1] = eyeRenderDesc[1].HmdToEyeViewOffset;

            // Create a mirror Texture
            mirrorTexture = hmd.CreateMirrorTexture(GraphicsDevice, GraphicsDevice.BackBuffer.Description);

            // Set presentation interval to immediate as SubmitFrame will be taking care of VSync
            GraphicsDevice.Presenter.PresentInterval = PresentInterval.Immediate;
            // Configure tracking
            hmd.ConfigureTracking(TrackingCapabilities.Orientation | TrackingCapabilities.Position | TrackingCapabilities.MagYawCorrection, TrackingCapabilities.None);
            // Set enabled capabilities
            hmd.EnabledCaps = HMDCapabilities.LowPersistence | HMDCapabilities.DynamicPrediction;

            
            base.Initialize();
        }

        public string ReadLineFromFile(string fname) {
            try {
                using (StreamReader sr = new StreamReader("../" + fname, Encoding.GetEncoding("Shift_JIS"))) {
                    string ret = sr.ReadLine();
                    return ret;
                }
            } catch (Exception e) {
                return "";
            }
        }

        protected override void LoadContent(){
            warai = Texture2D.Load(GraphicsDevice, "Content/laugh-man.bmp");
            camTexture[0] = camTexture[1] = warai;
            waraired = Texture2D.Load(GraphicsDevice, "Content/laugh-manred.bmp");

            // setting of SaveMode
            if (ReadLineFromFile("mode.conf") == "save"){
                SaveMode = true;
            }

            // setting of position
            string rlff = ReadLineFromFile("position.conf");
            if (rlff != "") {
                posname = rlff;
            }
            //Create Save Directory
            Directory.CreateDirectory("../textures/" + posname);
            for (int i = 0; i < 3; i++) {
                Directory.CreateDirectory("../textures/" + posname + "/" + i.ToString());
            }

            Console.WriteLine("\n\n\nMode: " + SaveMode + "\npos: " + posname);
            base.LoadContent();
        }

        // txt‚ðfpath‚É‘‚«o‚·
        public bool WriteFile(string fpath, string txt){
            using (StreamWriter sw = new StreamWriter(fpath, false, Encoding.ASCII)) {
                sw.Write(txt);
            }
            return true;
        }

        void GetAngle(){
            float floatYaw, floatPitch, floatRoll;
            hmd.GetEyePoses(0, hmdToEyeViewOffset, eyeRenderPose); //Šp“x‘ª‚Á‚Ä
            eyeRenderPose[0].Orientation.GetEulerAngles(out floatYaw, out floatPitch, out floatRoll); //’l‚ðŽæ‚èo‚µ‚Ä

            // convert to degree
            yaw = (int)(floatYaw * 180 *yawmag / Math.PI + 0.5);
            pitch = (int)(floatPitch * 180 *pitchmag / Math.PI + 0.5);
            roll = (int)(floatRoll * 180 * rollmag / Math.PI + 0.5);

            string CurrentPosition = (yaw + yawOffset).ToString() + "," + (pitch + pitchOffset).ToString();
            WriteFile(@"../CurrentPosition.txt", CurrentPosition);
        }
        
        void GenerateFileName(int eyeIndex, bool path=true){ 
            filename = "../textures/" + posname +"/" +eyeIndex + "/" + eyeIndex + "_" + (yaw + yawOffset).ToString() + "_" + (pitch + pitchOffset).ToString()  +".bmp"; //–¼‘O‚ð•t‚¯‚é
            Console.WriteLine(filename);
        }

        void ImageSave(int eyeIndex){
            GenerateFileName(eyeIndex);
            if (-rollrange <= roll && roll <= rollrange) {  //•Û‘¶‚·‚éðŒ
                cambmp.Save(filename); //‚Ù‚¼‚ñ
                string txtname = "2_" + (yaw + yawOffset).ToString() + "_" + (pitch + pitchOffset).ToString() + ".txt";
                WriteFile(@"../textures/" + posname + "/2/" + txtname, "");
                isStored[eyeIndex, yaw + yawOffset, pitch + pitchOffset] = true;

                Console.WriteLine(filename);
            } else {
                camTexture[eyeIndex] = waraired;
                Console.WriteLine(roll);
            }
        }

        protected override void Draw(GameTime gameTime) {
            hmd.GetEyePoses(0, hmdToEyeViewOffset, eyeRenderPose);
            layerEyeFov.RenderPoseLeft = eyeRenderPose[0];
            layerEyeFov.RenderPoseRight = eyeRenderPose[1];

            ovrcam.UpdateCamera();
            GetAngle();

            for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++){
                var eye = hmd.EyeRenderOrder[eyeIndex];
                var renderDesc = eyeRenderDesc[(int)eye];
                var renderPose = eyeRenderPose[(int)eye];


                //Get render target
                var swapTexture = eyeTexture[(int)eye];
                swapTexture.AdvanceToNextView();


                //Clear the screen

                GraphicsDevice.SetRenderTargets(swapTexture.DepthStencilView, swapTexture.CurrentView);
                GraphicsDevice.SetViewport(swapTexture.Viewport);
                Device.ImmediateContext.ClearDepthStencilView(swapTexture.DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1, 0);

                if (SaveMode){
                    //ƒJƒƒ‰Žæ“¾
                    if (eyeIndex == 1){
                        ovrcam.UpdateLeft();
                        cambmp = ovrcam.imageDataLeft;
                    } else {
                        ovrcam.UpdateRight();
                        cambmp = ovrcam.imageDataRight;
                    }

                    Console.WriteLine(yaw.ToString() + "," + pitch.ToString());
                    try { 
                        if (isStored[eyeIndex, yaw + yawOffset, pitch + pitchOffset]) {
                            camTexture[eyeIndex] = warai;
                        } else {
                            ImageSave(eyeIndex); //˜^‰æ
                            
                            camTexture[eyeIndex] = Texture2D.Load(GraphicsDevice, filename);
                        }
                    } catch (System.IO.IOException e) {
                        // Console.WriteLine(e.ToString());
                    }
                        
                }else{
                    try { //Ä¶‚ðtry
                        GenerateFileName(eyeIndex);
                        camTexture[eyeIndex] =Texture2D.Load(GraphicsDevice, filename);

                    } catch (System.IO.IOException e) {
                        Console.WriteLine("\n\nno photograph.\n\n");
                    }
                }

                // Perform the actual drawing
                var time = (float)gameTime.TotalGameTime.TotalSeconds;

                // DrawModel
                var sprite = new SpriteBatch(GraphicsDevice);
                sprite.Begin();
                sprite.Draw(camTexture[eyeIndex],
                    new Rectangle(0, 0, 1440, 1620), Color.White);
                sprite.End();
                    
                base.Draw(gameTime);
            }        
        }
        
        protected override void EndDraw() {
            // Oculus VR‚É“Š‚°‚é
            hmd.SubmitFrame(0, ref layerEyeFov.Header);
            GraphicsDevice.Copy(mirrorTexture, GraphicsDevice.BackBuffer);
            GraphicsDevice.Present();
        }

        protected override void Dispose(bool disposeManagedResources) {
            base.Dispose(disposeManagedResources);
            if (disposeManagedResources) {
                // Release the eye textures
                eyeTexture[0].Dispose();
                eyeTexture[1].Dispose();

                // Release the HMD
                hmd.Dispose();

                // Shutdown the OVR Library
                OVR.Shutdown();
            }   
        }
    }
}