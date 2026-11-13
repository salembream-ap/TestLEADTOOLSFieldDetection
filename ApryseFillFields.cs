using Leadtools.Forms.Processing.Omr;
using Leadtools.Forms.Processing.Omr.Fields;
using pdftron;
using pdftron.PDF;
using pdftron.PDF.Annots;
using pdftron.SDF;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace TestLEADTOOLSFieldDetection
{
   internal class ApryseFillFields
   {

      private static readonly PDFNetLoader PdfNetLoader =
          PDFNetLoader.Instance();

      public static void InitApryse()
      {
         var licPath = "C:\\Dev\\LicWebSalem\\Apryse.lic";
         PDFNet.Initialize(File.ReadAllText(licPath));
      }



      public static void GenerateFieldsPDF(
          string inputPdfPath,
          string outputPdfPath,
          IReadOnlyList<IReadOnlyList<TestField>> pagesFields,
          int fieldsResolution)
      {


         using var doc = new PDFDoc(inputPdfPath);
         doc.InitSecurityHandler();

         foreach (var (pageIndex, pageFields) in pagesFields.Index())
         {
            AddFieldsToPage(doc, pageIndex + 1, pageFields, fieldsResolution);
         }


         doc.Save(outputPdfPath, SDFDoc.SaveOptions.e_linearized);

      }
      public static void AddFieldsToPage(PDFDoc doc, int pageNumber, IReadOnlyList<TestField> fields, int fieldsResolution)
      {

         pdftron.PDF.Page page = doc.GetPage(pageNumber);

         if (page == null)
         {
            throw new InvalidOperationException(
                "The PDF does not contain a page");
         }

         double pageHeight = page.GetPageHeight();

         for (int i = 0; i < fields.Count; i++)
         {
            TestField item = fields[i];

            ValidateField(item, i);

            string fieldName = $"field_page_{pageNumber}_{i + 1}";

            /*
             * Input bounds:
             *
             *   X, Y
             *   ┌───────────┐
             *   │           │ Height
             *   └───────────┘
             *       Width
             *
             * where X/Y are measured from the page's top-left corner.
             *
             * PDF Rect coordinates are measured from the bottom-left.
             */


            // fix resolution for PDF coordinates (72 DPI).
            double resFactor = 72.0 / fieldsResolution;

            double x1 = item.Bounds.X * resFactor;
            double y1 = pageHeight - item.Bounds.Y * resFactor - item.Bounds.Height * resFactor;
            double x2 = item.Bounds.X * resFactor + item.Bounds.Width * resFactor;
            double y2 = pageHeight - item.Bounds.Y * resFactor;


            var rect = new Rect(x1, y1, x2, y2);

            // Logical AcroForm field.

            var fieldType = item.Type switch
            {
               TestFieldType.TextField => pdftron.PDF.Field.Type.e_text,
               TestFieldType.SplitTextField => pdftron.PDF.Field.Type.e_text,
               TestFieldType.CheckBox => pdftron.PDF.Field.Type.e_check,
               TestFieldType.RadioButton => pdftron.PDF.Field.Type.e_radio,
               _ => throw new NotSupportedException(
                   $"Unsupported field type: {item.Type}"),
            };

            pdftron.PDF.Field field = doc.FieldCreate(fieldName, fieldType);

            if (field.GetType() != fieldType)
            {
               throw new InvalidOperationException($"Failed to create field of type {fieldType}.");
            }

            if (item.Type == TestFieldType.SplitTextField && item.CombCharsLength > 0)
            {

               if (field.GetType() != fieldType)
               {
                  throw new InvalidOperationException($"Failed to create field of type {fieldType}.");
               }
               field.SetFlag(pdftron.PDF.Field.Flag.e_multiline, false);

               if (field.GetType() != fieldType)
               {
                  throw new InvalidOperationException($"Failed to create field of type {fieldType}.");
               }
               field.SetFlag(pdftron.PDF.Field.Flag.e_password, false);


               if (field.GetType() != fieldType)
               {
                  throw new InvalidOperationException($"Failed to create field of type {fieldType}.");
               }
               field.SetFlag(pdftron.PDF.Field.Flag.e_file_select, false);

               if (field.GetType() != fieldType)
               {
                  throw new InvalidOperationException($"Failed to create field of type {fieldType}.");
               }
               field.SetFlag(pdftron.PDF.Field.Flag.e_comb, true);

               if (field.GetType() != fieldType)
               {
                  throw new InvalidOperationException($"Failed to create field of type {fieldType}.");
               }
               field.SetMaxLen(item.CombCharsLength);

               if (field.GetType() != fieldType)
               {
                  throw new InvalidOperationException($"Failed to create field of type {fieldType}.");
               }
            }

            //field.SetFlag(pdftron.PDF.Field.Flag.e_required, false);


            if (field.GetType() != fieldType)
            {
               throw new InvalidOperationException($"Failed to create field of type {fieldType}.");
            }
            // Visible field widget on page 1.
            Widget widget = item.Type switch
            {
               TestFieldType.TextField => TextWidget.Create(doc, rect, field),
               TestFieldType.SplitTextField => TextWidget.Create(doc, rect, field),
               TestFieldType.CheckBox => CheckBoxWidget.Create(doc, rect, field),
               TestFieldType.RadioButton => RadioButtonWidget.Create(doc, rect, field),
               _ => throw new NotSupportedException(
                   $"Unsupported field type: {item.Type}"),

            };


            widget.SetFontSize(0); // Auto-size text.
            widget.RefreshAppearance();

            page.AnnotPushBack(widget);
         }

         // Regenerate appearances for all form fields.
         doc.RefreshFieldAppearances();

      }

      private static void ValidateField(TestField field, int index)
      {
         if (field.Bounds.Width <= 0)
         {
            throw new ArgumentOutOfRangeException(
                nameof(field.Bounds),
                $"Field at index {index} has an invalid width.");
         }

         if (field.Bounds.Height <= 0)
         {
            throw new ArgumentOutOfRangeException(
                nameof(field.Bounds),
                $"Field at index {index} has an invalid height.");
         }
      }

   }
}
