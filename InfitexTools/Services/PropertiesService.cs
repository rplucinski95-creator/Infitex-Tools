using System;
using InfitexTools.Models;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace InfitexTools.Services
{
    public class PropertiesService
    {
        public ModelProperties LoadModelProperties(ModelDoc2 model)
        {
            if (model == null) return null;

            return new ModelProperties
            {
                Index = GetProp(model, "Index"),
                PartNo = GetProp(model, "PartNo"),
                ActualRevision = GetProp(model, "ActualRevision"),
                FileType = GetFileType(model),
                CreatedBy = GetProp(model, "CreatedBy"),

                Description_EN = GetProp(model, "Description_EN"),
                Description_PL = GetProp(model, "Description_PL"),

                PartType = GetProp(model, "PartType"),
                Supplier = GetProp(model, "Supplier"),

                ProjectNumber = GetProp(model, "ProjectNumber"),
                ProjectName = GetProp(model, "ProjectName"),

                Remark = GetProp(model, "Remark"),
                DownloadLink = GetProp(model, "DownloadLink"),

                Comments = GetProp(model, "Comments")
            };
        }

        public void SaveModelProperties(ModelDoc2 model, ModelProperties props)
        {
            if (model == null || props == null) return;

            // Read-only / system-driven fields
            SetProp(model, "CreatedBy", props.CreatedBy ?? "");
            SetProp(model, "Description_EN", props.Description_EN ?? "");
            SetProp(model, "Description_PL", props.Description_PL ?? "");

            SetProp(model, "PartType", props.PartType ?? "");
            SetProp(model, "Supplier", props.Supplier ?? "");

            SetProp(model, "ProjectNumber", props.ProjectNumber ?? "");
            SetProp(model, "ProjectName", props.ProjectName ?? "");

            SetProp(model, "Remark", props.Remark ?? "");
            SetProp(model, "DownloadLink", props.DownloadLink ?? "");
            SetProp(model, "Comments", props.Comments ?? "");
        }

        public DrawingProperties LoadDrawingProperties(ModelDoc2 drawing)
        {
            if (drawing == null) return null;

            return new DrawingProperties
            {
                Index = GetProp(drawing, "Index"),
                Revision = GetProp(drawing, "ActualRevision"),

                Description_EN = GetProp(drawing, "Description_EN"),
                Description_PL = GetProp(drawing, "Description_PL"),

                ProjectNumber = GetProp(drawing, "ProjectNumber"),
                ProjectName = GetProp(drawing, "ProjectName"),

                ChangeStatus = GetProp(drawing, "ChangeStatus"),

                CreatedBy = GetProp(drawing, "CreatedBy"),
                CreatedDate = ParseDate(GetProp(drawing, "CreatedDate")),

                CheckedBy = GetProp(drawing, "CheckedBy"),
                CheckedDate = ParseDate(GetProp(drawing, "CheckedDate")),

                ApprovedBy = GetProp(drawing, "ApprovedBy"),
                ApprovedDate = ParseDate(GetProp(drawing, "ApprovedDate")),

                RevisionComment = GetProp(drawing, "RevisionComment"),
                RevisionDate = ParseDate(GetProp(drawing, "RevisionDate"))
            };
        }

        public void SaveDrawingProperties(ModelDoc2 drawing, DrawingProperties props)
        {
            if (drawing == null || props == null) return;

            SetProp(drawing, "Description_EN", props.Description_EN ?? "");
            SetProp(drawing, "Description_PL", props.Description_PL ?? "");

            SetProp(drawing, "ProjectNumber", props.ProjectNumber ?? "");
            SetProp(drawing, "ProjectName", props.ProjectName ?? "");

            SetProp(drawing, "ChangeStatus", props.ChangeStatus ?? "");

            SetProp(drawing, "CreatedBy", props.CreatedBy ?? "");
            SetProp(drawing, "CreatedDate", FormatDate(props.CreatedDate));

            SetProp(drawing, "CheckedBy", props.CheckedBy ?? "");
            SetProp(drawing, "CheckedDate", FormatDate(props.CheckedDate));

            SetProp(drawing, "ApprovedBy", props.ApprovedBy ?? "");
            SetProp(drawing, "ApprovedDate", FormatDate(props.ApprovedDate));

            SetProp(drawing, "RevisionComment", props.RevisionComment ?? "");
            SetProp(drawing, "RevisionDate", FormatDate(props.RevisionDate));
        }

        private string GetProp(ModelDoc2 model, string propName)
        {
            if (model == null) return "";

            try
            {
                CustomPropertyManager cust = model.Extension.CustomPropertyManager[""];
                if (cust == null) return "";

                string valOut = "";
                string resolvedOut = "";

                try
                {
                    cust.Get4(propName, false, out valOut, out resolvedOut);
                }
                catch
                {
                    try
                    {
                        cust.Get2(propName, out valOut, out resolvedOut);
                    }
                    catch
                    {
                        return "";
                    }
                }

                if (!string.IsNullOrWhiteSpace(resolvedOut))
                    return resolvedOut;

                return valOut ?? "";
            }
            catch
            {
                return "";
            }
        }

        private void SetProp(ModelDoc2 model, string propName, string value)
        {
            if (model == null) return;

            try
            {
                CustomPropertyManager cust = model.Extension.CustomPropertyManager[""];
                if (cust == null) return;

                cust.Add3(
                    propName,
                    (int)swCustomInfoType_e.swCustomInfoText,
                    value ?? "",
                    (int)swCustomPropertyAddOption_e.swCustomPropertyReplaceValue
                );
            }
            catch
            {
                // ignore for now
            }
        }

        private string GetFileType(ModelDoc2 model)
        {
            if (model == null) return "";

            try
            {
                int t = model.GetType();

                if (t == (int)swDocumentTypes_e.swDocPART) return "PART";
                if (t == (int)swDocumentTypes_e.swDocASSEMBLY) return "ASM";
                if (t == (int)swDocumentTypes_e.swDocDRAWING) return "DRW";
            }
            catch
            {
            }

            return "";
        }

        private DateTime ParseDate(string text)
        {
            DateTime dt;
            if (DateTime.TryParse(text, out dt))
                return dt;

            return DateTime.MinValue;
        }

        private string FormatDate(DateTime dt)
        {
            if (dt == DateTime.MinValue)
                return "";

            return dt.ToString("yyyy-MM-dd");
        }
    }
}