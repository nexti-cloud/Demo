using AnglickaVyzva.API.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class ExerciseVideo : IExercise
    {
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string NameCZ { get; set; }
        public string NameEN { get; set; }
        public bool HasError { get; set; }
        public string Type { get; set; } = "video";
        public bool IsLock { get; set; }
        public bool IsDone { get; set; }

        public string Subtitle { get; set; }
        public string VideoURL { get; set; }
        public string Image { get; set; }

        public ExerciseVideo(DataTable sheet, Section section, bool includeLocked)
        {
            try
            {
                if (sheet.Rows.Count < 1)
                {
                    HasError = true;
                    return;
                }

                string fullName = (string)sheet.Rows[0].ItemArray[0];
                NameCZ = fullName;

                // Obsahuje podnadpis
                if (NameCZ.Contains("~"))
                {
                    var parts = NameCZ.Split('~');
                    NameCZ = parts[0];
                    Subtitle = parts[1];
                }


                if (includeLocked == false)
                    IsLock = sheet.TableName.Contains("{P}") ? true : false;


                if (IsLock)
                    return;

                if (sheet.Rows.Count < 3)
                {
                    HasError = true;
                    return;
                }

                VideoURL = (string)sheet.Rows[2].ItemArray[0];
                Image = ImageHelper.CreateImage(sheet.Rows[2].ItemArray[1], section);

                if(sheet.Rows.Count > 3)
                {
                    if (sheet.Rows[3].ItemArray[0] != DBNull.Value)
                    {
                        try
                        {
                            Points = Convert.ToInt32(sheet.Rows[3].ItemArray[0]);
                        }
                        catch
                        {

                        }
                    }
                }
            }
            catch(Exception exc)
            {
                HasError = true;
            }
        }
    }
}
