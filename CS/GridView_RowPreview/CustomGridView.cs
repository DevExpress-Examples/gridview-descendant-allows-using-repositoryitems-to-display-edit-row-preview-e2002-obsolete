using System;
using System.Collections.Generic;
using System.Text;
using DevExpress.XtraGrid.Columns;
using DevExpress.Data.Filtering.Helpers;
using DevExpress.Data.Filtering;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid.Drawing;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Registrator;
using System.ComponentModel;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraEditors.ViewInfo;
using System.Drawing;
using DevExpress.Utils.Drawing;
using System.Windows.Forms;
using DevExpress.XtraGrid.Drawing;
using DevExpress.XtraGrid.Views.Grid.Handler;
using DevExpress.XtraEditors.Container;
using DevExpress.XtraEditors.Controls;

namespace CustomGrid_PreviewRow
{
    public class CustomGridView : GridView
    {
        protected RepositoryItem fRowPreviewEdit;
        bool isRowPreviewSelected;
        int postingEditorValue;
        public CustomGridView()
            : base()
        {
            fRowPreviewEdit = null;
            isRowPreviewSelected = false;
        }
        protected internal virtual void SetGridControlAccessMetod(GridControl newControl) { SetGridControl(newControl); }
        protected override string ViewName { get { return "CustomGridView"; } }
        [Category("Appearance"), Description("Gets or sets the repository item specifying the editor used to show row preview."), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible), 
        TypeConverter("DevExpress.XtraGrid.TypeConverters.ColumnEditConverter, " + AssemblyInfo.SRAssemblyGridDesign),
        Editor("DevExpress.XtraGrid.Design.ColumnEditEditor, " + AssemblyInfo.SRAssemblyGridDesign, typeof(System.Drawing.Design.UITypeEditor))]
        public DevExpress.XtraEditors.Repository.RepositoryItem PreviewRowEdit
        {
            get { return fRowPreviewEdit; }
            set
            {
                if (PreviewRowEdit != value)
                {
                    DevExpress.XtraEditors.Repository.RepositoryItem old = fRowPreviewEdit;
                    fRowPreviewEdit = value;
                    CustomGridViewInfo vi = ViewInfo as CustomGridViewInfo;
                    if (vi != null) vi.UpdateRowPreviewEdit(fRowPreviewEdit);
                    if (old != null) old.Disconnect(this);
                    if (PreviewRowEdit != null)
                    {
                        PreviewRowEdit.Connect(this);
                    }
                }
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (PreviewRowEdit != null)
            {
                PreviewRowEdit.Disconnect(this);
                this.fRowPreviewEdit = null;
            }
            base.Dispose(disposing);
        }
        public virtual object GetRowPreviewValue(int rowHandle)
        {
            object result = null;
            if (PreviewFieldName.Length != 0 && DataController.IsReady)
                result = DataController.GetRowValue(rowHandle, PreviewFieldName);
            if (result is string) return GetRowPreviewDisplayText(rowHandle);
            return result;
        }
        protected virtual internal bool IsRowPreviewSelected
        {
            get { return isRowPreviewSelected; }
            set
            {
                if (IsRowPreviewSelected != value)
                {
                    isRowPreviewSelected = value;
                    InvalidateRow(FocusedRowHandle);
                }
            }
        }
        protected virtual internal void ActivatePreviewEditor()
        {
            if (PreviewRowEdit == null) return;
            GridDataRowInfo ri = ViewInfo.GetGridRowInfo(FocusedRowHandle) as GridDataRowInfo;
            if (ri == null) return;
            Rectangle bounds = ((CustomGridViewInfo)ViewInfo).GetRowPreviewEditBounds(ri);
            bounds.Offset(ri.PreviewBounds.Location);
            UpdateEditor(PreviewRowEdit, new UpdateEditorInfoArgs(
                false, bounds, ri.AppearancePreview, GetRowPreviewValue(FocusedRowHandle), ElementsLookAndFeel, string.Empty, null));
        }

        protected virtual internal int RaiseMeasurePreviewHeightAccessMetod(int rowHandle) { return base.RaiseMeasurePreviewHeight(rowHandle); }
        protected override GridColumn GetNearestCanFocusedColumn(GridColumn col, int delta, bool allowChangeFocusedRow, KeyEventArgs e)
        {
            return base.GetNearestCanFocusedColumn(col, delta, allowChangeFocusedRow, e);
        }
        protected override bool PostEditor(bool causeValidation)
        {
            bool result = base.PostEditor(causeValidation);
            if (PreviewRowEdit == null) return result;
            if (this.postingEditorValue != 0) return result;
            try
            {
                this.postingEditorValue++;
                if (ActiveEditor == null || !EditingValueModified || this.fEditingCell != null) return result;
                if (causeValidation && !ValidateEditor()) return false;
                SetRowPreviewValueCore(FocusedRowHandle, EditingValue);
            }
            finally
            {
                this.postingEditorValue--;
            }
            return result;
        }
        protected override void CloseEditor(bool causeValidation)
        {
            IsRowPreviewSelected = false;
            base.CloseEditor(causeValidation);
        }
        private void SetRowPreviewValueCore(int rowHandle, object value)
        {
            if (PreviewRowEdit == null || FocusedColumn != null) return;
            try
            {
                DataController.SetRowValue(rowHandle, PreviewFieldName, value);
                UpdateRowAutoHeight(rowHandle);
                if (rowHandle == FocusedRowHandle && FocusedColumn == null)
                {
                    RefreshEditor(true);
                    SetFocusedRowModified();
                }
                Invalidate();
            }
            catch { }
        }
        protected override bool IsAutoHeight { get { if (PreviewRowEdit != null) return true; else return base.IsAutoHeight; } }
        public override GridColumn FocusedColumn
        {
            get { return base.FocusedColumn; }
            set
            {
                base.FocusedColumn = value;
                if (FocusedColumn != null) IsRowPreviewSelected = false;
            }
        }
    }
    public class CustomGridControl : GridControl
    {
        public CustomGridControl() : base() { }
        protected override void RegisterAvailableViewsCore(InfoCollection collection)
        {
            base.RegisterAvailableViewsCore(collection);
            collection.Add(new CustomGridInfoRegistrator());
        }
        protected override BaseView CreateDefaultView() { return CreateView("CustomGridView"); }
    }
    public class CustomGridPainter : GridPainter
    {
        public CustomGridPainter(GridView view) : base(view) { }
        public virtual new CustomGridView View { get { return (CustomGridView)base.View; } }
        protected override void DrawRowPreview(GridViewDrawArgs e, GridDataRowInfo ri)
        {
            RepositoryItem item = ((CustomGridView)e.ViewInfo.View).PreviewRowEdit;
            if (item == null)
                base.DrawRowPreview(e, ri);
            else
                DrawRowPreviewEditor(e, ri, item);
        }
        private void DrawRowPreviewEditor(GridViewDrawArgs e, GridDataRowInfo ri, RepositoryItem item)
        {
            GridCellInfo info = new GridCellInfo(null, ri, ri.PreviewBounds);
            info.Editor = item;
            DrawCellEdit(e, ((CustomGridViewInfo)e.ViewInfo).GetRowPreviewViewInfo(e, ri), info, ri.AppearancePreview, false);
        }
    }
    public class CustomGridViewInfo : GridViewInfo
    {
        BaseEditViewInfo fRowPreviewViewInfo;
        public CustomGridViewInfo(GridView gridView)
            : base(gridView)
        {
            UpdateRowPreviewEdit(View.PreviewRowEdit);
        }
        public virtual new CustomGridView View { get { return base.View as CustomGridView; } }
        public virtual void UpdateRowPreviewEdit(RepositoryItem item)
        {
            if (item != null)
                fRowPreviewViewInfo = CreateRowPreviewViewInfo(item);
            else
                fRowPreviewViewInfo = null;
        }
        protected virtual BaseEditViewInfo CreateRowPreviewViewInfo(RepositoryItem item)
        {
            BaseEditViewInfo info = item.CreateViewInfo();
            UpdateEditViewInfo(info);
            Graphics g = GInfo.AddGraphics(null);
            try
            {
                info.CalcViewInfo(g);
            }
            finally
            {
                GInfo.ReleaseGraphics();
            }
            return info;
        }
        public virtual BaseEditViewInfo GetRowPreviewViewInfo(GridViewDrawArgs e, GridDataRowInfo ri)
        {
            fRowPreviewViewInfo.Bounds = GetRowPreviewEditBounds(ri);
            fRowPreviewViewInfo.EditValue = View.GetRowPreviewValue(ri.RowHandle);
            fRowPreviewViewInfo.Focused = true;
            fRowPreviewViewInfo.CalcViewInfo(e.Graphics);
            return fRowPreviewViewInfo;
        }
        public virtual Rectangle GetRowPreviewEditBounds(GridDataRowInfo ri)
        {
            Rectangle r = new Rectangle(new Point(0, 0), ri.PreviewBounds.Size);            
            r.Inflate(-this.Painter.ElementsPainter.RowPreview.GetPreviewTextHorizontalPadding(this) / 2, -this.Painter.ElementsPainter.RowPreview.GetPreviewTextVerticalPadding(this) / 2);            
            return r;
        }
        public override int CalcRowPreviewHeight(int rowHandle)
        {
            RepositoryItem item = View.PreviewRowEdit;
            if (item == null)
                return base.CalcRowPreviewHeight(rowHandle);
            else
                return CalcRowPreviewEditorHeight(rowHandle, item);
        }
        protected virtual int CalcRowPreviewEditorHeight(int rowHandle, RepositoryItem item)
        {
            if (!View.OptionsView.ShowPreview || View.IsGroupRow(rowHandle) || View.IsFilterRow(rowHandle)) return 0;
            int res = (View.OptionsView.ShowPreviewRowLines == DevExpress.Utils.DefaultBoolean.False ? 0 : 1);
            int eventHeight = View.RaiseMeasurePreviewHeightAccessMetod(rowHandle);
            if (eventHeight != -1) return eventHeight == 0 ? 0 : res + eventHeight;
            Graphics g = GInfo.AddGraphics(null);
            try
            {
                IHeightAdaptable ha = fRowPreviewViewInfo as IHeightAdaptable;
                if (ha != null)
                {
                    fRowPreviewViewInfo.EditValue = View.GetRowPreviewValue(rowHandle);  
                    res = ha.CalcHeight(GInfo.Cache, this.CalcRowPreviewWidth(rowHandle) - this.Painter.ElementsPainter.RowPreview.GetPreviewIndent(this) - this.Painter.ElementsPainter.RowPreview.GetPreviewTextHorizontalPadding(this) * 2);
                }
                res = Math.Max(fRowPreviewViewInfo.CalcMinHeight(g), res);
            }
            finally
            {
                GInfo.ReleaseGraphics();
            }
            res += this.Painter.ElementsPainter.RowPreview.GetPreviewTextVerticalPadding(this);
            return res;
        }
        protected override void CalcRowHitInfo(Point pt, GridRowInfo ri, GridHitInfo hi)
        {
            base.CalcRowHitInfo(pt, ri, hi);
        }
    }
    public class CustomGridHandler : GridHandler
    {
        public CustomGridHandler(GridView gridView) : base(gridView) { }
        protected override GridRowNavigator CreateRowNavigator() { return new CustomGridRegularRowNavigator(this); }
    }
    public class CustomGridRegularRowNavigator : GridRegularRowNavigator
    {
        public CustomGridRegularRowNavigator(GridHandler handler) : base(handler) { }
        protected new CustomGridView View { get { return base.View as CustomGridView; } }
        public override bool OnMouseDown(GridHitInfo hitInfo, DevExpress.Utils.DXMouseEventArgs e)
        {
            bool res = base.OnMouseDown(hitInfo, e);
            if (hitInfo.HitTest == GridHitTest.RowPreview)
            {
                View.FocusedColumn = null;
                View.ActivatePreviewEditor();
                View.IsRowPreviewSelected = true;
            }
            else
                View.IsRowPreviewSelected = false;
            return res;
        }
    }
    public class CustomGridInfoRegistrator : GridInfoRegistrator
    {
        public CustomGridInfoRegistrator() : base() { }
        public override BaseViewPainter CreatePainter(BaseView view) { return new CustomGridPainter(view as GridView); }
        public override DevExpress.XtraGrid.Views.Base.ViewInfo.BaseViewInfo CreateViewInfo(BaseView view) { return new CustomGridViewInfo(view as GridView); }
        public override DevExpress.XtraGrid.Views.Base.Handler.BaseViewHandler CreateHandler(BaseView view) { return new CustomGridHandler(view as GridView); }
        public override string ViewName { get { return "CustomGridView"; } }
        public override BaseView CreateView(GridControl grid)
        {
            CustomGridView view = new CustomGridView();
            view.SetGridControlAccessMetod(grid);
            return view;
        }
    }
}
