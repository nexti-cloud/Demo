using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnglickaVyzva.AudioDownloader
{
    public class QuestionPart
    {
        public override string ToString()
        {
            return OriginalText;
        }

        public string OriginalText { get; set; }
        public Boolean IsInput { get; set; }
        public string InputText { get; set; }

        public QuestionPart(string originalText)
        {
            OriginalText = originalText;
            if(OriginalText == "_")
            {
                IsInput = true;
                InputText = "";
            }
        }

        /*
         originalText: string;
        isInput: boolean = false;
        inputText: string;

        constructor(originalText: string) {
            this.originalText = originalText;
            if (this.originalText == "_") {
                this.isInput = true;
                this.inputText = "";
            }
        }
         * */
    }
}
