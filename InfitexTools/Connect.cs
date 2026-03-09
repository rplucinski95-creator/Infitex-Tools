using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using InfitexTools.UI;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swpublished;
using SolidWorks.Interop.swconst;

namespace InfitexTools
{
    [ComVisible(true)]
    [Guid("01550087-99EC-4857-B0AF-05B552DA4036")]
    [ProgId("InfitexTools.Connect")]
    public class Connect : ISwAddin
    {
        private SldWorks _swApp;
        private int _addinCookie;

        private TaskpaneView _taskPaneView;
        private InfitexTaskPaneControl _taskPaneControl;

        private Timer _pollTimer;
        private string _lastDocKey = "";

        private const string LogPath = @"C:\InfitexAddinTest\addin_log.txt";
        private const string DummyChildText = "__DUMMY__";

        public bool ConnectToSW(object ThisSW, int Cookie)
        {
            try
            {
                File.AppendAllText(LogPath, DateTime.Now.ToString("s") + " ConnectToSW fired\r\n");

                _swApp = (SldWorks)ThisSW;
                _addinCookie = Cookie;
                _swApp.SetAddinCallbackInfo2(0, this, _addinCookie);

                CreateTaskPane();
                HookUi();
                StartPollingActiveDoc();

                File.AppendAllText(LogPath, DateTime.Now.ToString("s") + " TaskPane created\r\n");
                return true;
            }
            catch (Exception ex)
            {
                File.AppendAllText(LogPath, DateTime.Now.ToString("s") + " ERROR: " + ex + "\r\n");
                MessageBox.Show(ex.ToString(), "InfitexTools ERROR");
                return false;
            }
        }

        public bool DisconnectFromSW()
        {
            try
            {
                if (_pollTimer != null)
                {
                    _pollTimer.Stop();
                    _pollTimer.Dispose();
                    _pollTimer = null;
                }

                try
                {
                    if (_taskPaneView != null)
                        _taskPaneView.DeleteView();
                }
                catch { }

                _taskPaneView = null;
                _taskPaneControl = null;
                _swApp = null;

                return true;
            }
            catch
            {
                return true;
            }
        }

        private void CreateTaskPane()
        {
            string dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(dllPath);
            string iconPath = Path.Combine(folder, "PDM.bmp");

            if (!File.Exists(iconPath))
                iconPath = "";

            _taskPaneView = _swApp.CreateTaskpaneView2(iconPath, "Infitex Tools");
            _taskPaneControl = new InfitexTaskPaneControl();

            try
            {
                _taskPaneView.DisplayWindowFromHandlex64(_taskPaneControl.Handle.ToInt64());
            }
            catch
            {
                _taskPaneView.DisplayWindowFromHandle(_taskPaneControl.Handle.ToInt32());
            }
        }

        private void HookUi()
        {
            _taskPaneControl.RefreshRequested += () => RefreshTree();

            _taskPaneControl.SelectionChanged += tag =>
            {
                try
                {
                    UpdateDetailsPanel(tag);
                    UpdatePreviewPanel(tag);
                }
                catch
                {
                    _taskPaneControl.ClearDetails();
                    _taskPaneControl.ClearPreview();
                }
            };

            _taskPaneControl.NodeExpandRequested += nodeObj =>
            {
                var node = nodeObj as TreeNode;
                if (node == null) return;

                try
                {
                    PopulateNodeChildrenIfNeeded(node);
                }
                catch
                {
                    // ignore for now
                }
            };

            _taskPaneControl.PropertiesRequested += tag =>
            {
                try
                {
                    var comp = tag as Component2;
                    if (comp != null)
                    {
                        ModelDoc2 compModel = GetModelDocForComponent(comp);
                        if (compModel == null)
                        {
                            MessageBox.Show("Failed to resolve/open component model.", "Infitex Tools");
                            return;
                        }

                        int docType = compModel.GetType();

                        if (docType == (int)swDocumentTypes_e.swDocPART ||
                            docType == (int)swDocumentTypes_e.swDocASSEMBLY)
                        {
                            new PropertiesFormV2(compModel).Show();
                            return;
                        }

                        if (docType == (int)swDocumentTypes_e.swDocDRAWING)
                        {
                            MessageBox.Show("Properties v2 for drawings is not implemented yet.", "Infitex Tools");
                            return;
                        }

                        MessageBox.Show("Unsupported document type.", "Infitex Tools");
                        return;
                    }

                    var model = tag as ModelDoc2;
                    if (model != null)
                    {
                        int docType = model.GetType();

                        if (docType == (int)swDocumentTypes_e.swDocPART ||
                            docType == (int)swDocumentTypes_e.swDocASSEMBLY)
                        {
                            new PropertiesFormV2(model).Show();
                            return;
                        }

                        if (docType == (int)swDocumentTypes_e.swDocDRAWING)
                        {
                            MessageBox.Show("Properties v2 for drawings is not implemented yet.", "Infitex Tools");
                            return;
                        }

                        MessageBox.Show("Unsupported document type.", "Infitex Tools");
                        return;
                    }

                    MessageBox.Show("Unsupported node type.", "Infitex Tools");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Infitex Tools — Properties Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            _taskPaneControl.OpenRequested += tag =>
            {
                try
                {
                    OpenTagDocument(tag);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Infitex Tools — Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            _taskPaneControl.OpenDrawingRequested += tag =>
            {
                try
                {
                    OpenDrawingForTag(tag);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Infitex Tools — Open Drawing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
        }

        private void StartPollingActiveDoc()
        {
            _pollTimer = new Timer();
            _pollTimer.Interval = 700;
            _pollTimer.Tick += (s, e) =>
            {
                try
                {
                    var doc = _swApp != null ? (_swApp.ActiveDoc as ModelDoc2) : null;
                    var key = GetDocKey(doc);

                    if (key != _lastDocKey)
                    {
                        _lastDocKey = key;
                        RefreshTree();
                    }
                }
                catch { }
            };

            _pollTimer.Start();
            RefreshTree();
        }

        private string GetDocKey(ModelDoc2 doc)
        {
            if (doc == null) return "";

            string path = "";
            try { path = doc.GetPathName() ?? ""; } catch { }

            string title = "";
            try { title = doc.GetTitle() ?? ""; } catch { }

            return path + "||" + title;
        }

        private void RefreshTree()
        {
            var doc = _swApp != null ? (_swApp.ActiveDoc as ModelDoc2) : null;

            if (doc == null)
            {
                _taskPaneControl.SetRootNode(new TreeNode("No active document"));
                _taskPaneControl.ClearDetails();
                _taskPaneControl.ClearPreview();
                return;
            }

            int t = doc.GetType();

            if (t == (int)swDocumentTypes_e.swDocPART)
            {
                TreeNode root = BuildDocRootNode(doc, "part", "[PART]");
                _taskPaneControl.SetRootNode(root);
                return;
            }

            if (t == (int)swDocumentTypes_e.swDocDRAWING)
            {
                TreeNode root = BuildDocRootNode(doc, "drw", "[DRW]");
                _taskPaneControl.SetRootNode(root);
                return;
            }

            if (t == (int)swDocumentTypes_e.swDocASSEMBLY)
            {
                TreeNode asmRoot = BuildAssemblyRootNode(doc);
                _taskPaneControl.SetRootNode(asmRoot);
                return;
            }

            _taskPaneControl.SetRootNode(new TreeNode("Unsupported document type"));
            _taskPaneControl.ClearDetails();
            _taskPaneControl.ClearPreview();
        }

        private TreeNode BuildDocRootNode(ModelDoc2 doc, string imageKey, string prefix)
        {
            string path = "";
            try { path = doc.GetPathName() ?? ""; } catch { }

            string fileName = "";
            try { fileName = Path.GetFileName(path); } catch { }

            string index = GetModelProp(doc, "Index");
            string rev = GetModelProp(doc, "ActualRevision");
            string descEn = GetModelProp(doc, "Description_EN");

            string partDisplay = !string.IsNullOrWhiteSpace(index) ? index : fileName;
            string text = BuildNodeText(partDisplay, rev, descEn);

            var node = new TreeNode(prefix + " " + text)
            {
                Tag = doc,
                ImageKey = imageKey,
                SelectedImageKey = imageKey
            };

            return node;
        }

        private TreeNode BuildAssemblyRootNode(ModelDoc2 asmDoc)
        {
            string path = "";
            try { path = asmDoc.GetPathName() ?? ""; } catch { }

            string fileName = "";
            try { fileName = Path.GetFileName(path); } catch { }

            string index = GetModelProp(asmDoc, "Index");
            string rev = GetModelProp(asmDoc, "ActualRevision");
            string descEn = GetModelProp(asmDoc, "Description_EN");

            string partDisplay = !string.IsNullOrWhiteSpace(index) ? index : fileName;
            string text = BuildNodeText(partDisplay, rev, descEn);

            var root = new TreeNode("[ASM] " + text)
            {
                Tag = asmDoc,
                ImageKey = "asm",
                SelectedImageKey = "asm"
            };

            AddUniqueTopLevelChildren(root, asmDoc as AssemblyDoc);

            return root;
        }

        private void AddUniqueTopLevelChildren(TreeNode parentNode, AssemblyDoc asm)
        {
            if (asm == null) return;

            object compsObj = null;
            try { compsObj = asm.GetComponents(false); } catch { }

            object[] comps = compsObj as object[];
            if (comps == null) return;

            AddUniqueComponentChildren(parentNode, comps);
        }

        private void PopulateNodeChildrenIfNeeded(TreeNode node)
        {
            if (node.Nodes.Count != 1) return;
            if (node.Nodes[0].Text != DummyChildText) return;

            node.Nodes.Clear();

            var comp = node.Tag as Component2;
            if (comp == null) return;

            object kidsObj = null;
            try { kidsObj = comp.GetChildren(); } catch { }

            object[] kids = kidsObj as object[];
            if (kids == null) return;

            AddUniqueComponentChildren(node, kids);
        }

        private void AddUniqueComponentChildren(TreeNode parentNode, object[] rawChildren)
        {
            var map = new Dictionary<string, Component2>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < rawChildren.Length; i++)
            {
                var c = rawChildren[i] as Component2;
                if (c == null) continue;

                string key = GetUniqueKeyForBranch(c);
                if (string.IsNullOrWhiteSpace(key)) continue;

                if (!map.ContainsKey(key))
                    map[key] = c;
            }

            var keys = new List<string>(map.Keys);
            keys.Sort(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < keys.Count; i++)
            {
                Component2 c = map[keys[i]];
                TreeNode child = CreateComponentNode(c);
                parentNode.Nodes.Add(child);
            }
        }

        private TreeNode CreateComponentNode(Component2 comp)
        {
            string path = "";
            try { path = comp.GetPathName() ?? ""; } catch { }

            string fileName = BuildFileNameForComponent(comp, path);
            ModelDoc2 model = GetModelDocForComponent(comp);

            string index = GetModelProp(model, "Index");
            string rev = GetModelProp(model, "ActualRevision");
            string descEn = GetModelProp(model, "Description_EN");

            string partDisplay = !string.IsNullOrWhiteSpace(index) ? index : fileName;
            string text = BuildNodeText(partDisplay, rev, descEn);

            string imageKey = GetImageKeyFromPath(path);
            if (string.IsNullOrWhiteSpace(imageKey))
                imageKey = "part";

            var node = new TreeNode(text)
            {
                Tag = comp,
                ImageKey = imageKey,
                SelectedImageKey = imageKey
            };

            if (string.Equals(imageKey, "asm", StringComparison.OrdinalIgnoreCase) && HasAnyChildren(comp))
            {
                node.Nodes.Add(new TreeNode(DummyChildText));
            }

            return node;
        }

        private bool HasAnyChildren(Component2 comp)
        {
            try
            {
                object kidsObj = comp.GetChildren();
                object[] kids = kidsObj as object[];
                return kids != null && kids.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private string GetUniqueKeyForBranch(Component2 comp)
        {
            string path = "";
            try { path = comp.GetPathName() ?? ""; } catch { }

            if (!string.IsNullOrWhiteSpace(path))
                return path;

            try { return comp.Name2 ?? ""; } catch { return ""; }
        }

        private string BuildNodeText(string partDisplay, string rev, string descEn)
        {
            string p = partDisplay ?? "";
            string r = rev ?? "";
            string d = descEn ?? "";

            return p + " | " + r + " | " + d;
        }

        private void UpdateDetailsPanel(object tag)
        {
            string part = "";
            string rev = "";
            string descEn = "";
            string descPl = "";
            string status = "";
            string path = "";

            var comp = tag as Component2;
            if (comp != null)
            {
                try { path = comp.GetPathName() ?? ""; } catch { }

                var model = GetModelDocForComponent(comp);
                if (model != null)
                {
                    string fileName = BuildFileNameForComponent(comp, path);
                    string index = GetModelProp(model, "Index");

                    part = !string.IsNullOrWhiteSpace(index) ? index : fileName;
                    rev = GetModelProp(model, "ActualRevision");
                    descEn = GetModelProp(model, "Description_EN");
                    descPl = GetModelProp(model, "Description_PL");
                    status = GetModelProp(model, "Status");
                }

                _taskPaneControl.SetDetails(part, rev, descEn, descPl, status, path);
                return;
            }

            var doc = tag as ModelDoc2;
            if (doc != null)
            {
                try { path = doc.GetPathName() ?? ""; } catch { }

                string fileName = "";
                try { fileName = Path.GetFileName(path); } catch { }

                string index = GetModelProp(doc, "Index");
                part = !string.IsNullOrWhiteSpace(index) ? index : fileName;
                rev = GetModelProp(doc, "ActualRevision");
                descEn = GetModelProp(doc, "Description_EN");
                descPl = GetModelProp(doc, "Description_PL");
                status = GetModelProp(doc, "Status");

                _taskPaneControl.SetDetails(part, rev, descEn, descPl, status, path);
                return;
            }

            _taskPaneControl.ClearDetails();
        }

        private void UpdatePreviewPanel(object tag)
        {
            string key = "part";

            var comp = tag as Component2;
            if (comp != null)
            {
                string path = "";
                try { path = comp.GetPathName() ?? ""; } catch { }

                key = GetImageKeyFromPath(path);
                var img = _taskPaneControl.GetLargePreviewIcon(key);
                _taskPaneControl.SetPreviewImage(img);
                return;
            }

            var doc = tag as ModelDoc2;
            if (doc != null)
            {
                int t = doc.GetType();

                if (t == (int)swDocumentTypes_e.swDocASSEMBLY) key = "asm";
                else if (t == (int)swDocumentTypes_e.swDocDRAWING) key = "drw";
                else key = "part";

                var img = _taskPaneControl.GetLargePreviewIcon(key);
                _taskPaneControl.SetPreviewImage(img);
                return;
            }

            _taskPaneControl.ClearPreview();
        }

        private string BuildFileNameForComponent(Component2 comp, string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    var fn = Path.GetFileName(path);
                    if (!string.IsNullOrWhiteSpace(fn)) return fn;
                }
                catch { }
            }

            try { return comp.Name2 ?? "(component)"; }
            catch { return "(component)"; }
        }

        private string GetImageKeyFromPath(string path)
        {
            string p = (path ?? "").ToLowerInvariant();
            if (p.EndsWith(".sldasm")) return "asm";
            if (p.EndsWith(".slddrw")) return "drw";
            return "part";
        }

        private ModelDoc2 GetModelDocForComponent(Component2 comp)
        {
            if (comp == null) return null;

            try
            {
                var loaded = comp.GetModelDoc2() as ModelDoc2;
                if (loaded != null) return loaded;
            }
            catch { }

            try
            {
                string path = comp.GetPathName() ?? "";
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return null;

                int docType = GetDocTypeFromPath(path);
                int errs = 0, warns = 0;

                return _swApp.OpenDoc6(
                    path,
                    docType,
                    (int)swOpenDocOptions_e.swOpenDocOptions_Silent | (int)swOpenDocOptions_e.swOpenDocOptions_ReadOnly,
                    "",
                    ref errs,
                    ref warns
                ) as ModelDoc2;
            }
            catch
            {
                return null;
            }
        }

        private void OpenTagDocument(object tag)
        {
            var comp = tag as Component2;
            if (comp != null)
            {
                string path = "";
                try { path = comp.GetPathName() ?? ""; } catch { }

                if (string.IsNullOrWhiteSpace(path))
                {
                    MessageBox.Show("Component path is unavailable.", "Infitex Tools");
                    return;
                }

                OpenDocumentByPath(path);
                return;
            }

            var model = tag as ModelDoc2;
            if (model != null)
            {
                string path = "";
                try { path = model.GetPathName() ?? ""; } catch { }

                if (!string.IsNullOrWhiteSpace(path))
                {
                    OpenDocumentByPath(path);
                    return;
                }
            }
        }

        private void OpenDrawingForTag(object tag)
        {
            string modelPath = GetPathFromTag(tag);

            if (string.IsNullOrWhiteSpace(modelPath))
            {
                MessageBox.Show("Model path is unavailable.", "Infitex Tools");
                return;
            }

            string drawingPath = Path.ChangeExtension(modelPath, ".SLDDRW");

            if (!File.Exists(drawingPath))
            {
                MessageBox.Show("Drawing not found:\n" + drawingPath, "Infitex Tools");
                return;
            }

            OpenDocumentByPath(drawingPath);
        }

        private string GetPathFromTag(object tag)
        {
            var comp = tag as Component2;
            if (comp != null)
            {
                try { return comp.GetPathName() ?? ""; } catch { return ""; }
            }

            var model = tag as ModelDoc2;
            if (model != null)
            {
                try { return model.GetPathName() ?? ""; } catch { return ""; }
            }

            return "";
        }

        private void OpenDocumentByPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                MessageBox.Show("File not found:\n" + path, "Infitex Tools");
                return;
            }

            int docType = GetDocTypeFromPath(path);
            int errs = 0, warns = 0;

            var doc = _swApp.OpenDoc6(
                path,
                docType,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                "",
                ref errs,
                ref warns
            ) as ModelDoc2;

            if (doc == null)
            {
                MessageBox.Show("Failed to open file.\nErrors: " + errs + "\nWarnings: " + warns, "Infitex Tools");
                return;
            }

            try
            {
                _swApp.ActivateDoc3(doc.GetTitle(), true, (int)swRebuildOnActivation_e.swRebuildActiveDoc, ref errs);
            }
            catch { }
        }

        private string GetModelProp(ModelDoc2 model, string propName)
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

                if (!string.IsNullOrWhiteSpace(resolvedOut)) return resolvedOut;
                return valOut ?? "";
            }
            catch
            {
                return "";
            }
        }

        private int GetDocTypeFromPath(string path)
        {
            string p = (path ?? "").ToLowerInvariant();

            if (p.EndsWith(".sldprt")) return (int)swDocumentTypes_e.swDocPART;
            if (p.EndsWith(".sldasm")) return (int)swDocumentTypes_e.swDocASSEMBLY;
            if (p.EndsWith(".slddrw")) return (int)swDocumentTypes_e.swDocDRAWING;

            return (int)swDocumentTypes_e.swDocNONE;
        }
    }
}