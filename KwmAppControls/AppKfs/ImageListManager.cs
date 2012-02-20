using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using kwm.Utils;

namespace kwm.KwmAppControls.AppKfs
{
    public class ImageListManager
    {
        private ImageList m_imgList;

        /// <summary>
        /// Set this to true if this is an imagelist for small icons,
        /// false otherwise.
        /// </summary>
        private bool m_small = true;

        public ImageList ImgList
        {
            get
            {
                return m_imgList;
            }
        }

        public ImageListManager()
        {
            m_imgList = new ImageList();
            if (m_small)
            {
                m_imgList.ImageSize = new Size(16, 16);
            }
            else
            {
                m_imgList.ImageSize = new Size(32, 32);
            }

            m_imgList.ColorDepth = ColorDepth.Depth32Bit;
        }

        public ImageListManager(bool _small)
            : this()
        {
            m_small = _small;
        }

        public void Clear()
        {
            m_imgList.Images.Clear();
        }

        /// <summary>
        /// Manually add an icon to the list. If the key already exists,
        /// overwrite the value.
        /// </summary>
        /// <param name="_icon"></param>
        /// <param name="key"></param>
        public void AddIcon(Bitmap _icon, String key)
        {
            m_imgList.Images.Add(key, _icon);
        }

        /// <summary>
        /// If the key is not already in the list, add an icon to the list at the given key
        /// and return that key.
        /// If the key already exists, just return the key.
        /// Don't forget to call Clear() before updating the listview in order
        /// to have up-to-date icons. Or not, to be much more efficient but
        /// outdated if the icons change (i.e. kwm.exe remote only will appear
        /// with the default .exe icon, but once it will get downloaded, it should
        /// appear with the Teambox icon).
        /// 
        /// The idea is to get the specific file icon if the file exists, or
        /// to get the icon associated to the extension otherwise.
        /// </summary>
        /// <param name="fileFsPath"></param>
        /// <returns></returns>
        public String GetImageKey(String fileFsPath)
        {
            if (File.Exists(fileFsPath))
            {
                if (!m_imgList.Images.ContainsKey(fileFsPath))
                {
                    m_imgList.Images.Add(fileFsPath, ExtractIcon.GetIcon(fileFsPath, m_small));
                }

                return fileFsPath;
            }
            else
            {
                // The file does not exist. We know we are asking for the
                // icon of a generic extension.
                if (fileFsPath == "")
                    fileFsPath = "teamboxemptyextension";

                if (!m_imgList.Images.ContainsKey(fileFsPath))
                {
                    m_imgList.Images.Add(fileFsPath, ExtractIcon.GetIcon(fileFsPath, m_small));
                }
                return fileFsPath;
            }
        }
    }
}
