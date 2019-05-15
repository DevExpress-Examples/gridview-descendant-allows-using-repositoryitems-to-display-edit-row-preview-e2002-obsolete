Imports System
Imports System.Collections.Generic
Imports System.Text
Imports DevExpress.XtraGrid.Columns
Imports DevExpress.Data.Filtering.Helpers
Imports DevExpress.Data.Filtering
Imports DevExpress.XtraGrid.Views.Grid
Imports DevExpress.XtraGrid
Imports DevExpress.XtraGrid.Views.Grid.Drawing
Imports DevExpress.XtraGrid.Views.Grid.ViewInfo
Imports DevExpress.XtraGrid.Views.Base
Imports DevExpress.XtraGrid.Registrator
Imports System.ComponentModel
Imports DevExpress.XtraEditors.Repository
Imports DevExpress.XtraEditors.ViewInfo
Imports System.Drawing
Imports DevExpress.Utils.Drawing
Imports System.Windows.Forms
Imports DevExpress.XtraGrid.Drawing
Imports DevExpress.XtraGrid.Views.Grid.Handler
Imports DevExpress.XtraEditors.Container
Imports DevExpress.XtraEditors.Controls

Namespace CustomGrid_PreviewRow
	Public Class CustomGridView
		Inherits GridView

		Protected fRowPreviewEdit As RepositoryItem
'INSTANT VB NOTE: The field isRowPreviewSelected was renamed since Visual Basic does not allow fields to have the same name as other class members:
		Private isRowPreviewSelected_Renamed As Boolean
		Private postingEditorValue As Integer
		Public Sub New()
			MyBase.New()
			fRowPreviewEdit = Nothing
			isRowPreviewSelected_Renamed = False
		End Sub
		Protected Friend Overridable Sub SetGridControlAccessMetod(ByVal newControl As GridControl)
			SetGridControl(newControl)
		End Sub
		Protected Overrides ReadOnly Property ViewName() As String
			Get
				Return "CustomGridView"
			End Get
		End Property
		<Category("Appearance"), Description("Gets or sets the repository item specifying the editor used to show row preview."), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible), TypeConverter("DevExpress.XtraGrid.TypeConverters.ColumnEditConverter, " & AssemblyInfo.SRAssemblyGridDesign), Editor("DevExpress.XtraGrid.Design.ColumnEditEditor, " & AssemblyInfo.SRAssemblyGridDesign, GetType(System.Drawing.Design.UITypeEditor))>
		Public Property PreviewRowEdit() As DevExpress.XtraEditors.Repository.RepositoryItem
			Get
				Return fRowPreviewEdit
			End Get
			Set(ByVal value As DevExpress.XtraEditors.Repository.RepositoryItem)
				If PreviewRowEdit IsNot value Then
					Dim old As DevExpress.XtraEditors.Repository.RepositoryItem = fRowPreviewEdit
					fRowPreviewEdit = value
					Dim vi As CustomGridViewInfo = TryCast(ViewInfo, CustomGridViewInfo)
					If vi IsNot Nothing Then
						vi.UpdateRowPreviewEdit(fRowPreviewEdit)
					End If
					If old IsNot Nothing Then
						old.Disconnect(Me)
					End If
					If PreviewRowEdit IsNot Nothing Then
						PreviewRowEdit.Connect(Me)
					End If
				End If
			End Set
		End Property
		Protected Overrides Sub Dispose(ByVal disposing As Boolean)
			If PreviewRowEdit IsNot Nothing Then
				PreviewRowEdit.Disconnect(Me)
				Me.fRowPreviewEdit = Nothing
			End If
			MyBase.Dispose(disposing)
		End Sub
		Public Overridable Function GetRowPreviewValue(ByVal rowHandle As Integer) As Object
			Dim result As Object = Nothing
			If PreviewFieldName.Length <> 0 AndAlso DataController.IsReady Then
				result = DataController.GetRowValue(rowHandle, PreviewFieldName)
			End If
			If TypeOf result Is String Then
				Return GetRowPreviewDisplayText(rowHandle)
			End If
			Return result
		End Function
		Protected Friend Overridable Property IsRowPreviewSelected() As Boolean
			Get
				Return isRowPreviewSelected_Renamed
			End Get
			Set(ByVal value As Boolean)
				If IsRowPreviewSelected <> value Then
					isRowPreviewSelected_Renamed = value
					InvalidateRow(FocusedRowHandle)
				End If
			End Set
		End Property
		Protected Friend Overridable Sub ActivatePreviewEditor()
			If PreviewRowEdit Is Nothing Then
				Return
			End If
			Dim ri As GridDataRowInfo = TryCast(ViewInfo.GetGridRowInfo(FocusedRowHandle), GridDataRowInfo)
			If ri Is Nothing Then
				Return
			End If
			Dim bounds As Rectangle = CType(ViewInfo, CustomGridViewInfo).GetRowPreviewEditBounds(ri)
			bounds.Offset(ri.PreviewBounds.Location)
			UpdateEditor(PreviewRowEdit, New UpdateEditorInfoArgs(False, bounds, ri.AppearancePreview, GetRowPreviewValue(FocusedRowHandle), ElementsLookAndFeel, String.Empty, Nothing))
		End Sub

		Protected Friend Overridable Function RaiseMeasurePreviewHeightAccessMetod(ByVal rowHandle As Integer) As Integer
			Return MyBase.RaiseMeasurePreviewHeight(rowHandle)
		End Function
		Protected Overrides Function GetNearestCanFocusedColumn(ByVal col As GridColumn, ByVal delta As Integer, ByVal allowChangeFocusedRow As Boolean, ByVal e As KeyEventArgs) As GridColumn
			Return MyBase.GetNearestCanFocusedColumn(col, delta, allowChangeFocusedRow, e)
		End Function
		Protected Overrides Function PostEditor(ByVal causeValidation As Boolean) As Boolean
			Dim result As Boolean = MyBase.PostEditor(causeValidation)
			If PreviewRowEdit Is Nothing Then
				Return result
			End If
			If Me.postingEditorValue <> 0 Then
				Return result
			End If
			Try
				Me.postingEditorValue += 1
				If ActiveEditor Is Nothing OrElse Not EditingValueModified OrElse Me.fEditingCell IsNot Nothing Then
					Return result
				End If
				If causeValidation AndAlso Not ValidateEditor() Then
					Return False
				End If
				SetRowPreviewValueCore(FocusedRowHandle, EditingValue)
			Finally
				Me.postingEditorValue -= 1
			End Try
			Return result
		End Function
		Protected Overrides Sub CloseEditor(ByVal causeValidation As Boolean)
			IsRowPreviewSelected = False
			MyBase.CloseEditor(causeValidation)
		End Sub
		Private Sub SetRowPreviewValueCore(ByVal rowHandle As Integer, ByVal value As Object)
			If PreviewRowEdit Is Nothing OrElse FocusedColumn IsNot Nothing Then
				Return
			End If
			Try
				DataController.SetRowValue(rowHandle, PreviewFieldName, value)
				UpdateRowAutoHeight(rowHandle)
				If rowHandle = FocusedRowHandle AndAlso FocusedColumn Is Nothing Then
					RefreshEditor(True)
					SetFocusedRowModified()
				End If
				Invalidate()
			Catch
			End Try
		End Sub
		Protected Overrides ReadOnly Property IsAutoHeight() As Boolean
			Get
				If PreviewRowEdit IsNot Nothing Then
					Return True
				Else
					Return MyBase.IsAutoHeight
				End If
			End Get
		End Property
		Public Overrides Property FocusedColumn() As GridColumn
			Get
				Return MyBase.FocusedColumn
			End Get
			Set(ByVal value As GridColumn)
				MyBase.FocusedColumn = value
				If FocusedColumn IsNot Nothing Then
					IsRowPreviewSelected = False
				End If
			End Set
		End Property
	End Class
	Public Class CustomGridControl
		Inherits GridControl

		Public Sub New()
			MyBase.New()
		End Sub
		Protected Overrides Sub RegisterAvailableViewsCore(ByVal collection As InfoCollection)
			MyBase.RegisterAvailableViewsCore(collection)
			collection.Add(New CustomGridInfoRegistrator())
		End Sub
		Protected Overrides Function CreateDefaultView() As BaseView
			Return CreateView("CustomGridView")
		End Function
	End Class
	Public Class CustomGridPainter
		Inherits GridPainter

		Public Sub New(ByVal view As GridView)
			MyBase.New(view)
		End Sub
		Public Overridable Shadows ReadOnly Property View() As CustomGridView
			Get
				Return CType(MyBase.View, CustomGridView)
			End Get
		End Property
		Protected Overrides Sub DrawRowPreview(ByVal e As GridViewDrawArgs, ByVal ri As GridDataRowInfo)
			Dim item As RepositoryItem = CType(e.ViewInfo.View, CustomGridView).PreviewRowEdit
			If item Is Nothing Then
				MyBase.DrawRowPreview(e, ri)
			Else
				DrawRowPreviewEditor(e, ri, item)
			End If
		End Sub
		Private Sub DrawRowPreviewEditor(ByVal e As GridViewDrawArgs, ByVal ri As GridDataRowInfo, ByVal item As RepositoryItem)
			Dim info As New GridCellInfo(Nothing, ri, ri.PreviewBounds)
			info.Editor = item
			DrawCellEdit(e, CType(e.ViewInfo, CustomGridViewInfo).GetRowPreviewViewInfo(e, ri), info, ri.AppearancePreview, False)
		End Sub
	End Class
	Public Class CustomGridViewInfo
		Inherits GridViewInfo

		Private fRowPreviewViewInfo As BaseEditViewInfo
		Public Sub New(ByVal gridView As GridView)
			MyBase.New(gridView)
			UpdateRowPreviewEdit(View.PreviewRowEdit)
		End Sub
		Public Overridable Shadows ReadOnly Property View() As CustomGridView
			Get
				Return TryCast(MyBase.View, CustomGridView)
			End Get
		End Property
		Public Overridable Sub UpdateRowPreviewEdit(ByVal item As RepositoryItem)
			If item IsNot Nothing Then
				fRowPreviewViewInfo = CreateRowPreviewViewInfo(item)
			Else
				fRowPreviewViewInfo = Nothing
			End If
		End Sub
		Protected Overridable Function CreateRowPreviewViewInfo(ByVal item As RepositoryItem) As BaseEditViewInfo
			Dim info As BaseEditViewInfo = item.CreateViewInfo()
			UpdateEditViewInfo(info)
			Dim g As Graphics = GInfo.AddGraphics(Nothing)
			Try
				info.CalcViewInfo(g)
			Finally
				GInfo.ReleaseGraphics()
			End Try
			Return info
		End Function
		Public Overridable Function GetRowPreviewViewInfo(ByVal e As GridViewDrawArgs, ByVal ri As GridDataRowInfo) As BaseEditViewInfo
			fRowPreviewViewInfo.Bounds = GetRowPreviewEditBounds(ri)
			fRowPreviewViewInfo.EditValue = View.GetRowPreviewValue(ri.RowHandle)
			fRowPreviewViewInfo.Focused = True
			fRowPreviewViewInfo.CalcViewInfo(e.Graphics)
			Return fRowPreviewViewInfo
		End Function
		Public Overridable Function GetRowPreviewEditBounds(ByVal ri As GridDataRowInfo) As Rectangle
			Dim r As New Rectangle(New Point(0, 0), ri.PreviewBounds.Size)
'INSTANT VB WARNING: Instant VB cannot determine whether both operands of this division are integer types - if they are then you should use the VB integer division operator:
			r.Inflate(-Me.Painter.ElementsPainter.RowPreview.GetPreviewTextHorizontalPadding(Me) / 2, -Me.Painter.ElementsPainter.RowPreview.GetPreviewTextVerticalPadding(Me) / 2)
			Return r
		End Function
		Public Overrides Function CalcRowPreviewHeight(ByVal rowHandle As Integer) As Integer
			Dim item As RepositoryItem = View.PreviewRowEdit
			If item Is Nothing Then
				Return MyBase.CalcRowPreviewHeight(rowHandle)
			Else
				Return CalcRowPreviewEditorHeight(rowHandle, item)
			End If
		End Function
		Protected Overridable Function CalcRowPreviewEditorHeight(ByVal rowHandle As Integer, ByVal item As RepositoryItem) As Integer
			If Not View.OptionsView.ShowPreview OrElse View.IsGroupRow(rowHandle) OrElse View.IsFilterRow(rowHandle) Then
				Return 0
			End If
			Dim res As Integer = (If(View.OptionsView.ShowPreviewRowLines = DevExpress.Utils.DefaultBoolean.False, 0, 1))
			Dim eventHeight As Integer = View.RaiseMeasurePreviewHeightAccessMetod(rowHandle)
			If eventHeight <> -1 Then
				Return If(eventHeight = 0, 0, res + eventHeight)
			End If
			Dim g As Graphics = GInfo.AddGraphics(Nothing)
			Try
				Dim ha As IHeightAdaptable = TryCast(fRowPreviewViewInfo, IHeightAdaptable)
				If ha IsNot Nothing Then
					fRowPreviewViewInfo.EditValue = View.GetRowPreviewValue(rowHandle)
					res = ha.CalcHeight(GInfo.Cache, Me.CalcRowPreviewWidth(rowHandle) - Me.Painter.ElementsPainter.RowPreview.GetPreviewIndent(Me) - Me.Painter.ElementsPainter.RowPreview.GetPreviewTextHorizontalPadding(Me) * 2)
				End If
				res = Math.Max(fRowPreviewViewInfo.CalcMinHeight(g), res)
			Finally
				GInfo.ReleaseGraphics()
			End Try
			res += Me.Painter.ElementsPainter.RowPreview.GetPreviewTextVerticalPadding(Me)
			Return res
		End Function
		Protected Overrides Sub CalcRowHitInfo(ByVal pt As Point, ByVal ri As GridRowInfo, ByVal hi As GridHitInfo)
			MyBase.CalcRowHitInfo(pt, ri, hi)
		End Sub
	End Class
	Public Class CustomGridHandler
		Inherits GridHandler

		Public Sub New(ByVal gridView As GridView)
			MyBase.New(gridView)
		End Sub
		Protected Overrides Function CreateRowNavigator() As GridRowNavigator
			Return New CustomGridRegularRowNavigator(Me)
		End Function
	End Class
	Public Class CustomGridRegularRowNavigator
		Inherits GridRegularRowNavigator

		Public Sub New(ByVal handler As GridHandler)
			MyBase.New(handler)
		End Sub
		Protected Shadows ReadOnly Property View() As CustomGridView
			Get
				Return TryCast(MyBase.View, CustomGridView)
			End Get
		End Property
		Public Overrides Function OnMouseDown(ByVal hitInfo As GridHitInfo, ByVal e As DevExpress.Utils.DXMouseEventArgs) As Boolean
			Dim res As Boolean = MyBase.OnMouseDown(hitInfo, e)
			If hitInfo.HitTest = GridHitTest.RowPreview Then
				View.FocusedColumn = Nothing
				View.ActivatePreviewEditor()
				View.IsRowPreviewSelected = True
			Else
				View.IsRowPreviewSelected = False
			End If
			Return res
		End Function
	End Class
	Public Class CustomGridInfoRegistrator
		Inherits GridInfoRegistrator

		Public Sub New()
			MyBase.New()
		End Sub
		Public Overrides Function CreatePainter(ByVal view As BaseView) As BaseViewPainter
			Return New CustomGridPainter(TryCast(view, GridView))
		End Function
		Public Overrides Function CreateViewInfo(ByVal view As BaseView) As DevExpress.XtraGrid.Views.Base.ViewInfo.BaseViewInfo
			Return New CustomGridViewInfo(TryCast(view, GridView))
		End Function
		Public Overrides Function CreateHandler(ByVal view As BaseView) As DevExpress.XtraGrid.Views.Base.Handler.BaseViewHandler
			Return New CustomGridHandler(TryCast(view, GridView))
		End Function
		Public Overrides ReadOnly Property ViewName() As String
			Get
				Return "CustomGridView"
			End Get
		End Property
		Public Overrides Function CreateView(ByVal grid As GridControl) As BaseView
			Dim view As New CustomGridView()
			view.SetGridControlAccessMetod(grid)
			Return view
		End Function
	End Class
End Namespace
