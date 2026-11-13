
using Leadtools;
using Leadtools.ImageProcessing;
using Leadtools.Document;
using Leadtools.ImageProcessing.Core;

namespace TestLEADTOOLSFieldDetection
{
   public enum TestFieldType
   {
      TextField,
      SplitTextField,
      CheckBox,
      RadioButton,

   }
   public class TestField
   {
      public TestFieldType Type { get; set; }
      public LeadRect Bounds;

      public int CombCharsLength { get; set; } = 0;
   }


   internal class Program
   {
      static void Main(string[] args)
      {
         LTFields.InitLT();
         ApryseFillFields.InitApryse();

         var pdfPath = "C:/Users/SalemAl-Bream/Downloads/Current_Account_Form.pdf";
         var extractedFieldsPerPage = LTFields.ExtractFields(pdfPath, 300);

         ApryseFillFields.GenerateFieldsPDF(
             pdfPath,
             Path.Combine(Path.GetDirectoryName(pdfPath) ?? "./", "Current_Account_Form_with_fields_generated.pdf"),
             extractedFieldsPerPage,
             300);

         Console.WriteLine("Done!");
      }
   }
}
