    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using SharpDX;
    using Windows.UI.Xaml.Media.Imaging;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.IO;
    using System.Diagnostics;
    
    namespace ChickenRasterizer.Graphics
    {
        public class Device
        {
            private byte[] backBuffer;
            private WriteableBitmap bmp;
            
            /* 
              #0 I have no idea how this is going to be used, I am assuming it's taking a stored/input bitmap
              and redrawing it. That being said is there a specific sequence that the methods/funcions can be called?
              e.g. Can I do a Device() -> .Clear() -> Present() -> PutPixel(). Based on this answer I would have to 
              update/add some comments. 
            */
    
            /* 
               #1 Again, probably a comprehension issue from my side, but is it really necessary for your class
                  to initialize and simultaneously maintain the bitmap itself and backBuffer when it's created? 
                  Please remember your object is always holding this data is going to be heavy. From what I see backBuffer 
                  is only used during PutPixel and Present. You could think about trade offs and whether you need to consider
                  either memory or delay during rendering. eg. you could create the backbuffer and put pixels from bmp into it 
                  during Present (not sure if my assumption on what this code does is totally off). For mobile code, which I assume                   this is, what you've chosen sorta makes sense but please think about this, you don't want crashes.
               #2 [Test] Can WriteableBitmap be null/empty? If yes, should your constructor gracefully handle that?
               #3 [Test] Can WriteableBitmap be corrupted?
               #4 [Test] Do you want to do a size limit check on the size of the bmp and reject it if too big?
                  e.g. Someone could try to set a huge bmp and your byte array's size could exceed the max value.
            */
            public Device(WriteableBitmap bmp)
            {
                this.bmp = bmp;
                backBuffer = new byte[bmp.PixelWidth * bmp.PixelHeight * 4];
            }
			      
            /* 
                #5 I was a bit thrown off by the *4 iteration but it seems fine and like it won't overflow. 
                   However is there a better way to do this? Something C# counterparts of Java's as Arrays.fill or 
                   Collections.nCopies? In short, is there a util that does this already?
            */
            public void Clear(byte r, byte g, byte b, byte a)
            {
                for (int index = 0; index < backBuffer.Length; index += 4)
                {
                    backBuffer[index] = b;
                    backBuffer[index + 1] = g;
                    backBuffer[index + 2] = r;
                    backBuffer[index + 3] = a;
                }
            }
			      
            /*
               #6 Question: What is going on this method?
               Re-rendering both bmp and backBuffer? 
            */
            public void Present()
            {
                using(var stream = bmp.PixelBuffer.AsStream())
                {
                    stream.Write(backBuffer, 0, backBuffer.Length);
                }
                bmp.Invalidate();
            }
            
            /*
               #7 So I assume this just keeps the bmp and backBuffer in sync, yea?
                  If that is the case, should this method even be public?
                  The public question/comment applies to PutPixel, Project, DrawLine
                  DrawPoint.
            */
            public void PutPixel(int x, int y, Color4 color)
            {
                int index = (x + y * bmp.PixelWidth) * 4;
    
                backBuffer[index] = (byte)(color.Blue * 255);
                backBuffer[index + 1] = (byte)(color.Green * 255);
                backBuffer[index + 2] = (byte)(color.Red * 255);
                backBuffer[index + 3] = (byte)(color.Alpha * 255);
            }
            
            /*
                if you've validated your bmp and are sure there would be no divide by zero errors etc, then I don't have any
                comments.
                [Test] Boundaries
            */
            public Vector2 Project(Vector3 coord, Matrix transMat)
            {
                var point = Vector3.TransformCoordinate(coord, transMat);
                var x = point.X * bmp.PixelWidth + bmp.PixelWidth / 2.0f;
                var y = -point.Y * bmp.PixelHeight + bmp.PixelHeight / 2.0f;
                return (new Vector2(x, y));
            }
			
            /*
               #8 [Test] Boundaries - 
                  Are you trying to draw points between positions excluding the positions themselves?
                  please validate/test with p1-p0 odd and even.
                  eg. 1, 5 draws points at 3, 2, 4 and skips 1, 5.
                      1, 6 draws points at 3, 2, 4, 5 and skips 1, 5.
            */
            public void DrawLine(Vector2 p0,Vector2 p1)
            {
                float dist = (p1 - p0).Length();
                if(dist < 2)
                {
                    return;
                }
                Vector2 middlePoint = p0 + (p1 - p0) / 2;
                DrawPoint(middlePoint);
    
                DrawLine(p0, middlePoint);
                DrawLine(middlePoint, p1);
            }
            
            //I like the validation on this one, Question: Does this method also need to take a Color input?
            public void DrawPoint(Vector2 point)
            {
                if (point.X >= 0 && point.Y >= 0 && point.X < bmp.PixelWidth && point.Y < bmp.PixelHeight)
                {
                    PutPixel((int)point.X, (int)point.Y, new Color4(1.0f, 1.0f, 0.0f, 1.0f));
                }
            }
            
            /*
              OK I think I kind of get an idea of what this class does after reading this method.
              From a unit testing POV as long as you've tested boundary conditions (for the arrays)
              at Project, DrawPoint, well you should be fine.
            */
             
            public void Render(Camera cam, params Mesh[] meshes)
            {
                Matrix viewMatrix = Matrix.LookAtLH(cam.Position, cam.Target, Vector3.UnitY);
                Matrix projectionMatrix = Matrix.PerspectiveFovLH(0.78f, (float)bmp.PixelWidth / bmp.PixelHeight, 1f, 10000.0f);
    
                foreach(Mesh mesh in meshes)
                {
                    Matrix worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y,mesh.Rotation.X, mesh.Rotation.Z) * Matrix.Translation(mesh.Position);
                    Matrix transform = worldMatrix * viewMatrix * projectionMatrix;
    
                    for (var i = 0; i < mesh.Vertices.Length - 1; i++)
                    {
                        var point0 = Project(mesh.Vertices[i], transform);
                        var point1 = Project(mesh.Vertices[i + 1], transform);
                        DrawLine(point0, point1);
                    }
                }
            }
        }
    }
