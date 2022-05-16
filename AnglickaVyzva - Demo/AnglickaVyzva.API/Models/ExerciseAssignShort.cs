﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public class ExerciseAssignShort : IExercise
    {
        public int Order { get; set; }
        public int Points { get; set; } = 1;
        public string NameCZ { get; set; }
        public string NameEN { get; set; }
        public bool HasError { get; set; }
        public string Type { get; set; } = "assign_short";
        public bool IsLock { get; set; }
        public bool IsDone { get; set; }

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<ItemAssignShort_Select> ItemList { get; set; }

        public class ItemAssignShort_Select
        {
            public int ID { get; set; }
            public string First { get; set; }
            public string Second { get; set; }
        }

        public ExerciseAssignShort(DataTable sheet, Section section, bool includeLocked)
        {
            if (sheet.Rows[2].ItemArray[0] != DBNull.Value)
            {
                try
                {
                    Points = Convert.ToInt32(sheet.Rows[2].ItemArray[0]);
                }
                catch
                {

                }
            }
            ItemList = new List<ItemAssignShort_Select>();

            NameCZ = (string)sheet.Rows[0].ItemArray[0];

            // Obsahuje podnadpis
            if (NameCZ.Contains("~"))
            {
                var parts = NameCZ.Split('~');
                NameCZ = parts[0];
                Subtitle = parts[1];
            }

            Title = (string)sheet.Rows[1].ItemArray[0];

            if (includeLocked == false)
                IsLock = sheet.TableName.Contains("{P}") ? true : false;

            if (IsLock)
                return;

            for (int i = 3; i < sheet.Rows.Count; i++)
            {
                DataRow row = sheet.Rows[i];
                ItemAssignShort_Select item = new ItemAssignShort_Select();

                if (i > 4 && row.ItemArray[1] == DBNull.Value)
                {
                    break;
                }

                item.ID = Convert.ToInt32(row.ItemArray[0]);

                item.First = row.ItemArray[1] == System.DBNull.Value ? "" : Convert.ToString(row.ItemArray[1]);

                item.Second = row.ItemArray[2] == System.DBNull.Value ? "" : Convert.ToString(row.ItemArray[2]);

                ItemList.Add(item);
            }
        }
    }
}
