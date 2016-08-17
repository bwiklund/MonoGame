// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics
{
	public partial class TextureCube : Texture
	{
		internal int size;

        /// <summary>
        /// Gets the width and height of the cube map face in pixels.
        /// </summary>
        /// <value>The width and height of a cube map face in pixels.</value>
        public int Size
        {
            get
            {
                return size;
            }
        }
		
		public TextureCube (GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format)
            : this(graphicsDevice, size, mipMap, format, false)
		{
        }

        internal TextureCube(GraphicsDevice graphicsDevice, int size, bool mipMap, SurfaceFormat format, bool renderTarget)
        {
            if (graphicsDevice == null)
                throw new ArgumentNullException("graphicsDevice", FrameworkResources.ResourceCreationWhenDeviceIsNull);
            if (size <= 0)
                throw new ArgumentOutOfRangeException("size","Cube size must be greater than zero");

            this.GraphicsDevice = graphicsDevice;
			this.size = size;
            this._format = format;
            this._levelCount = mipMap ? CalculateMipLevels(size) : 1;

            PlatformConstruct(graphicsDevice, size, mipMap, format, renderTarget);
        }

        /// <summary>
        /// Gets a copy of cube texture data specifying a cubemap face.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cubeMapFace">The cube map face.</param>
        /// <param name="data">The data.</param>
        public void GetData<T>(CubeMapFace cubeMapFace, T[] data) where T : struct
        {
            PlatformGetData<T>(cubeMapFace, data);
        }

	    public void GetData<T>(CubeMapFace cubeMapFace, T[] data, int startIndex, int elementCount) where T : struct
	    {
	        
	    }

	    public void GetData<T>(CubeMapFace cubeMapFace, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
	    {
            ValidateParams(level, rect, data, startIndex, elementCount);
	        
	    }

		public void SetData<T> (CubeMapFace face, T[] data) where T : struct
		{
            SetData(face, 0, null, data, 0, data.Length);
		}

        public void SetData<T>(CubeMapFace face, T[] data, int startIndex, int elementCount) where T : struct
		{
            SetData(face, 0, null, data, startIndex, elementCount);
		}
		
        public void SetData<T>(CubeMapFace face, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
		{
            ValidateParams(level, rect, data, startIndex, elementCount);

            var elementSizeInByte = Utilities.ReflectionHelpers.SizeOf<T>.Get();
            var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            // Use try..finally to make sure dataHandle is freed in case of an error
            try
            {
                var dataPtr = (IntPtr)(dataHandle.AddrOfPinnedObject().ToInt64() + startIndex * elementSizeInByte);

                int xOffset, yOffset, width, height;
                if (rect.HasValue)
                {
                    xOffset = rect.Value.X;
                    yOffset = rect.Value.Y;
                    width = rect.Value.Width;
                    height = rect.Value.Height;
                }
                else
                {
                    xOffset = 0;
                    yOffset = 0;
                    width = Math.Max(1, this.size >> level);
                    height = Math.Max(1, this.size >> level);

                    // For DXT textures the width and height of each level is a multiple of 4.
                    // OpenGL only: The last two mip levels require the width and height to be 
                    // passed as 2x2 and 1x1, but there needs to be enough data passed to occupy 
                    // a 4x4 block. 
                    // Ref: http://www.mentby.com/Group/mac-opengl/issue-with-dxt-mipmapped-textures.html 
                    if (_format == SurfaceFormat.Dxt1 ||
                        _format == SurfaceFormat.Dxt1SRgb ||
                        _format == SurfaceFormat.Dxt1a ||
                        _format == SurfaceFormat.Dxt3 ||
                        _format == SurfaceFormat.Dxt3SRgb ||
                        _format == SurfaceFormat.Dxt5 ||
                        _format == SurfaceFormat.Dxt5SRgb)
                    {
#if DIRECTX
                        width = (width + 3) & ~3;
                        height = (height + 3) & ~3;
#else
                        if (width > 4)
                            width = (width + 3) & ~3;
                        if (height > 4)
                            height = (height + 3) & ~3;
#endif
                    }
                }
                PlatformSetData<T>(face, level, dataPtr, xOffset, yOffset, width, height);
            }
            finally
            {
                dataHandle.Free();
            }
		}

        private void ValidateParams<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
        {
            var textureBounds = new Rectangle(0, 0, Math.Max(Size >> level, 1), Math.Max(Size >> level, 1));
            var checkedRect = rect.HasValue ? rect.Value : textureBounds;
            if (level < 0 || level >= LevelCount)
                throw new ArgumentException("level must be smaller than the number of levels in this texture.");
            if (!textureBounds.Contains(checkedRect))
                throw new ArgumentException("Rectangle must be inside the texture bounds", "rect");
            if (data == null)
                throw new ArgumentNullException("data");
            var tSize = Marshal.SizeOf(typeof(T));
            var fSize = Format.GetSize();
            if (tSize > fSize || fSize % tSize != 0)
                throw new ArgumentException("Type T is of an invalid size for the format of this texture.", "T");
            if (startIndex < 0 || startIndex >= data.Length)
                throw new ArgumentException("startIndex must be at least zero and smaller than data.Length.", "startIndex");
            if (data.Length < startIndex + elementCount)
                throw new ArgumentException("The data array is too small.");
            if (elementCount * tSize != checkedRect.Width * checkedRect.Height * fSize)
                throw new ArgumentException("elementCount is too large or too small.", "elementCount");
        }
	}
}

