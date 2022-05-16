using AnglickaVyzva.API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Helpers
{
    public class ImageHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageCell">Bunka excelu</param>
        /// <returns></returns>
        public static string CreateImage(object imageCell, Section section)
        {
            if (imageCell == System.DBNull.Value)
                return "";

            string image = section.DataFolderPath + Convert.ToString(imageCell);

            var extension = Path.GetExtension(image).ToLower();
            if (extension != ".png" && extension != ".jpg")
                image += ".png";

            return image;
        }
    }
}
