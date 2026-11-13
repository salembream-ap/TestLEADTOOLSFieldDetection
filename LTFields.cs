using Leadtools;
using Leadtools.Document;
using Leadtools.ImageProcessing.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestLEADTOOLSFieldDetection
{
   internal class LTFields
   {

      /// <summary>
      /// Combine every two or more consecutive marks as one TextField.
      /// </summary>
      public static List<TestField> HelperCombineOMRMarksIntoTextField(List<TestField> fields)
      {
         var result = new List<TestField>();
         var marks = new List<TestField>();

         foreach (var field in fields)
         {
            if (field.Type == TestFieldType.CheckBox || field.Type == TestFieldType.RadioButton)
               marks.Add(field);
            else
               result.Add(field);
         }

         // Reading order: top to bottom, then left to right.
         marks.Sort((a, b) =>
         {
            int byY = CenterY(a.Bounds).CompareTo(CenterY(b.Bounds));
            return byY != 0 ? byY : a.Bounds.Left.CompareTo(b.Bounds.Left);
         });

         var used = new bool[marks.Count];

         for (int i = 0; i < marks.Count; i++)
         {
            if (used[i])
               continue;

            // Grow a chain of consecutive marks starting at i, going left to right on one line.
            var chain = new List<TestField> { marks[i] };
            used[i] = true;

            while (true)
            {
               var last = chain[^1];
               int next = -1;
               int bestGap = int.MaxValue;

               for (int j = i + 1; j < marks.Count; j++)
               {
                  if (used[j] || !AreConsecutive(last.Bounds, marks[j].Bounds))
                     continue;

                  int gap = marks[j].Bounds.Left - last.Bounds.Right;
                  if (gap < bestGap)
                  {
                     bestGap = gap;
                     next = j;
                  }
               }

               if (next < 0)
                  break;

               chain.Add(marks[next]);
               used[next] = true;
            }

            if (chain.Count < 2)
            {
               result.Add(chain[0]);
               continue;
            }

            int left = chain.Min(f => f.Bounds.Left);
            int top = chain.Min(f => f.Bounds.Top);
            int right = chain.Max(f => f.Bounds.Right);
            int bottom = chain.Max(f => f.Bounds.Bottom);

            result.Add(new TestField
            {
               Type = TestFieldType.SplitTextField,
               Bounds = LeadRect.FromLTRB(left, top, right, bottom),
               CombCharsLength = chain.Count
            });
         }

         return result;
      }

      public static double CenterY(LeadRect r) => (r.Top + r.Bottom) / 2.0;

      /// <summary>
      /// True when <paramref name="next"/> sits to the right of <paramref name="prev"/> on the same line,
      /// has a similar size, and is separated by no more than about one mark width.
      /// </summary>
      public static bool AreConsecutive(LeadRect prev, LeadRect next)
      {
         int minHeight = Math.Min(prev.Height, next.Height);
         int maxHeight = Math.Max(prev.Height, next.Height);
         int minWidth = Math.Min(prev.Width, next.Width);
         int maxWidth = Math.Max(prev.Width, next.Width);

         if (minHeight <= 0 || minWidth <= 0)
            return false;

         // Similar size.
         if (maxHeight > minHeight * 1.5 || maxWidth > minWidth * 1.5)
            return false;

         // Same line: centers vertically aligned within half a mark height.
         if (Math.Abs(CenterY(prev) - CenterY(next)) > minHeight * 0.5)
            return false;

         // Directly to the right with a small gap (slight overlap tolerated for touching comb boxes).
         int gap = next.Left - prev.Right;
         return gap >= -minWidth * 0.25 && gap <= maxWidth;
      }

      public static List<List<TestField>> ExtractFields(string pdfPath, int resolution)
      {
         var results = new List<List<TestField>>();

         using var document = DocumentFactory.LoadFromFile(pdfPath, new LoadDocumentOptions());

         var fieldsCommand = new Leadtools.ImageProcessing.Core.FindCandidateFormFieldsCommand();

         foreach (var page in document.Pages)
         {
            var rasterImage = page.GetImage(resolution);
            fieldsCommand.Run(rasterImage);

            var pageFields = new List<TestField>();

            foreach (var field in fieldsCommand.TextFieldAreas)
            {
               var testField = new TestField
               {
                  Bounds = field.Bounds,
                  Type = TestFieldType.TextField,
               };

               pageFields.Add(testField);
            }

            foreach (var field in fieldsCommand.OMRFieldAreas)
            {
               var testField = new TestField
               {
                  Type = field.FieldType == OMRFieldType.BoxOMR ? TestFieldType.CheckBox : TestFieldType.RadioButton,
                  Bounds = field.UnfilledBound,
               };

               pageFields.Add(testField);
            }

            pageFields = HelperCombineOMRMarksIntoTextField(pageFields);

            results.Add(pageFields);

         }
         return results;

      }
      public static void InitLT()
      {
         var LicPath = "C:\\Dev\\LicWebSalem\\LEADTOOLS.lic";
         var KeyPath = LicPath + ".key";

         // Initialize LEADTOOLS
         Leadtools.RasterSupport.SetLicense(LicPath, File.ReadAllText(KeyPath));
         if (Leadtools.RasterSupport.KernelExpired)
         {
            throw new Exception("LEADTOOLS license is expired or invalid.");
         }

      }
   }
}
