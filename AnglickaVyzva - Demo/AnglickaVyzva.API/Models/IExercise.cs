using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Models
{
    public interface IExercise
    {
        int Order { get; set; } //Poradi
        int Points { get; set; }
        string NameCZ { get; set; }
        string NameEN { get; set; }
        bool HasError { get; set; }
        string Type { get; set; }
        bool IsLock { get; set; }
        bool IsDone { get; set; }
        string Subtitle { get; set; } // Napr.: NameCZ = Přeložte do Aj~procvičujete Věci okolo nás  -> "procvičujete Věci okolo nás" je podnadpis

        public class Types
        {
            public const string ExerciseAssing = "assign";
            public const string ExerciseAssignPicture = "assign_picture";
            public const string ExerciseAssignShort = "assign_short";
            public const string ExerciseAudio_SelectPicture = "audio_select_picture";
            public const string ExerciseColumns = "columns";
            public const string ExerciseConversation = "conversation";
            public const string ExerciseDecision = "decision";
            public const string ExerciseDictation = "dictation";
            public const string ExerciseDictation_Fill = "dictation_fill";
            public const string ExerciseDictation_Select = "dictation_select";
            public const string ExerciseFill = "fill";
            public const string ExerciseFill_Picture = "fill_picture";
            public const string ExerciseFillTable = "fillTable";
            public const string ExerciseGame_Columns = "game_columns";
            public const string ExerciseGame_Dice = "game_dice";
            public const string ExerciseInsert = "insert";
            public const string ExerciseOptions = "options";
            public const string ExercisePicture_Columns = "picture_columns";
            public const string ExercisePicture_FulltextReply = "picture_fulltextReply";
            public const string ExercisePicture_Select = "picture_select";
            public const string ExerciseQuestion_FulltextReply = "question_fulltextReply";
            public const string ExerciseRewrite = "rewrite";
            public const string ExerciseRows = "rows";
            public const string ExerciseSelect = "select";
            public const string ExerciseSelect_Picture = "select_picture";
            public const string ExerciseSelect_PictureOptions = "select_pictureoptions";
            public const string ExerciseSort = "sort";
            public const string ExerciseTwo_Sentences = "two_sentences";
            public const string ExerciseVideo = "video";
            public const string ExerciseVocabulary = "vocabulary";
        }
    }
}
