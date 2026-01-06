using CefSharp;
using CefSharp.OffScreen;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StardustCraft.UI;
using CefSharp.Structs;
using Size = System.Drawing.Size;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;
using System.Security.Cryptography.X509Certificates;
using CefSharp.Handler;
using CefSharp.Internals;

namespace StardustCraft.Browser
{
    
    public class GameBrowser
    {
        public ChromiumWebBrowser browser;

        private int browserTexture;
        private IntPtr pendingBufferHandle;
        private int pendingWidth;
        private int pendingHeight;
        private bool hasNewFrame;
        private readonly object bufferLock = new();
        GameBridge bridge;
       
        public GameBrowser(int width, int height, string url)
        {
          
            var browserSettings = new CefSharp.BrowserSettings
            {
                BackgroundColor = 0x00000000,
                
            };
            browser = new ChromiumWebBrowser(url, browserSettings)
            {
                Size = new Size(width, height),
                
            };
           
            bridge = new(this);
            browser.Paint += OnBrowserPaint;
            browser.JavascriptObjectRepository.ResolveObject += (sender, e) =>
            {
                var repo = e.ObjectRepository;
                if (e.ObjectName == "gameBridge")
                {
                    repo.Register("gameBridge", bridge, null);
                }
            };
            browser.LoadError += (sender, args) =>
            {
                Console.WriteLine($"Error while loading {args.FailedUrl}: {args.ErrorText}");
               
                new Thread(() =>
                {
                    bridge.Close();

                }).Start();
            };
            // browser.JavascriptObjectRepository.Register("HgGameJsBridge", new HgGameBridge(this), options: BindingOptions.DefaultBinder);
            browser.RequestHandler = new ReqHandler();
            
            // Crea texture OpenGL
            CreateBrowserTexture(width, height);
        }

       
        public class ReqHandler : RequestHandler
        {
          
            public bool OnBeforeBrowse(IWebBrowser browser, IRequest request, bool isRedirect)
            {
                if (request.TransitionType == TransitionType.ForwardBack)
                {
                    return true;
                }
                return false;
            }
        }
        private byte[] bufferCopy;

        void OnBrowserPaint(object sender, OnPaintEventArgs e)
        {
            lock (bufferLock)
            {
                int bufferSize = e.Width * e.Height * 4; // BGRA = 4 byte
                if (bufferCopy == null || bufferCopy.Length != bufferSize)
                    bufferCopy = new byte[bufferSize];

                System.Runtime.InteropServices.Marshal.Copy(e.BufferHandle, bufferCopy, 0, bufferSize);

                pendingWidth = e.Width;
                pendingHeight = e.Height;
                hasNewFrame = true;
            }
        }

        private void UploadToOpenGLTexture(byte[] buffer, int width, int height)
        {
            GL.BindTexture(TextureTarget.Texture2D, browserTexture);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

            unsafe
            {
                fixed (byte* ptr = buffer)
                {
                    GL.TexSubImage2D(
                        TextureTarget.Texture2D,
                        0,
                        0, 0,
                        width,
                        height,
                        PixelFormat.Bgra,
                        PixelType.UnsignedByte,
                        (IntPtr)ptr
                    );
                }
            }

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        private void CreateBrowserTexture(int width, int height)
        {
            if (browserTexture != 0)
            {
                GL.DeleteTexture(browserTexture);
            }

            browserTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, browserTexture);

            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                width,
                height,
                0,
                PixelFormat.Bgra,
                PixelType.UnsignedByte,
                IntPtr.Zero
            );

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public void Render(int windowWidth, int windowHeight)
        {
            // Resize browser se necessario
            if (browser.Size.Width != windowWidth || browser.Size.Height != windowHeight)
            {
                browser.Size = new Size(windowWidth, windowHeight);

                // Ricrea texture con nuova dimensione
                CreateBrowserTexture(windowWidth, windowHeight);
            }

            // Upload nuovo frame se disponibile
            if (hasNewFrame)
            {
                lock (bufferLock)
                {
                    UploadToOpenGLTexture(bufferCopy, pendingWidth, pendingHeight);
                    hasNewFrame = false;
                }
            }

            // Disegna la texture (qui puoi usare il tuo UserInterface.RenderQuad)
            UserInterface.RenderQuad(new Vector2(), new Vector2(windowWidth, windowHeight),Vector4.One, browserTexture);
        }

        public void HandleMouseMove(int x, int y)
        {
            if (!browser.IsBrowserInitialized) return;
            browser.GetBrowser()?.GetHost()?.SendMouseMoveEvent(x,y,false, GetKeyboardModifiers());
        }

        public void HandleMouseClick(int x, int y, MouseButton button, bool mouseUp)
        {
            if (!browser.IsBrowserInitialized) return;
            MouseButtonType btnType = button switch
            {
                MouseButton.Left => CefSharp.MouseButtonType.Left,
                MouseButton.Right => CefSharp.MouseButtonType.Right,
                MouseButton.Middle => CefSharp.MouseButtonType.Middle,
                _ => CefSharp.MouseButtonType.Left
            };
            browser.GetBrowser()?.GetHost()?.SendMouseClickEvent(x,y,btnType,mouseUp,1, GetKeyboardModifiers());
        }

        public void HandleMouseWheel(int deltaX, int deltaY, int x, int y)
        {
            if (!browser.IsBrowserInitialized) return;
            browser.GetBrowser()?.GetHost()?.SendMouseWheelEvent(x,y,deltaX,deltaY, GetKeyboardModifiers());
        }
        private CefEventFlags GetKeyboardModifiers()
        {
            CefEventFlags flags = 0;

            if (Game.Instance.KeyboardState.IsKeyDown(Keys.LeftShift) ||
                Game.Instance.KeyboardState.IsKeyDown(Keys.RightShift))
                flags |= CefEventFlags.ShiftDown;

            if (Game.Instance.KeyboardState.IsKeyDown(Keys.LeftControl) ||
                Game.Instance.KeyboardState.IsKeyDown(Keys.RightControl))
                flags |= CefEventFlags.ControlDown;

            if (Game.Instance.KeyboardState.IsKeyDown(Keys.LeftAlt) ||
                Game.Instance.KeyboardState.IsKeyDown(Keys.RightAlt))
                flags |= CefEventFlags.AltDown;

            return flags;
        }
        public void HandleKeyPress(Keys key, bool isDown)
        {
            if (!browser.IsBrowserInitialized) return;
            var keyEvent = new KeyEvent
            {
                WindowsKeyCode = (int)MapKeyToWindowsKeyCode(key), // OpenTK keycode -> Cef code
                FocusOnEditableField = true,
                IsSystemKey = false,
                Type = isDown ? KeyEventType.KeyDown : KeyEventType.KeyUp
            };

            browser.GetBrowser()?.GetHost()?.SendKeyEvent(keyEvent);
            if (key == Keys.F12)
            {
                browser.ShowDevTools();
            }
        }
        private int MapKeyToWindowsKeyCode(Keys key)
        {
            return key switch
            {
                Keys.Backspace => 0x08, // VK_BACK
                Keys.Tab => 0x09,       // VK_TAB
                Keys.Enter => 0x0D,     // VK_RETURN
                Keys.Escape => 0x1B,    // VK_ESCAPE
                Keys.Space => 0x20,     // VK_SPACE
                Keys.Delete => 0x2E,    // VK_DELETE
                Keys.Left => 0x25,      // VK_LEFT
                Keys.Up => 0x26,        // VK_UP
                Keys.Right => 0x27,     // VK_RIGHT
                Keys.Down => 0x28,      // VK_DOWN
                _ => (int)key // per lettere/numeri funge direttamente
            };
        }
        public void HandleChar(char c)
        {
            if (!browser.IsBrowserInitialized) return;
            var keyEvent = new KeyEvent
            {
                Type = KeyEventType.Char,
                WindowsKeyCode = c,
                FocusOnEditableField = true
            };

            browser.GetBrowser()?.GetHost()?.SendKeyEvent(keyEvent);
        }
    }
}
