// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static System.String;
using System.Windows;
using System.Diagnostics.Eventing.Reader;


#if BRICS
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Bricscad.EditorInput;
using Teigha.Geometry;
using Teigha.Runtime;
using CadApp = Bricscad.ApplicationServices.Application;
using AOpenDialog = Bricscad.Windows.OpenFileDialog;
using RtException = Teigha.Runtime.Exception;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AOpenDialog = Autodesk.AutoCAD.Windows.OpenFileDialog;
#endif

namespace AVC
{
  internal partial class
  BoxFromTableBox : OptionsBox
  {
    public
    BoxFromTableBox()
    {
      InitializeComponent();
      if (FormEditorMode) return;
#if VARS || DEBUG
      CommandNames = new string[] { DbCommand.BoxFromTableCmdName, DbCommand.BoxToWallCmdName, DbCommand.BoxToVectorCmdName };
#else
      CommandNames = new string[] { DbCommand.BoxFromTableCmdName};
#endif
    }

    public override bool
    SaveData()
    {
      if (Loading || Disposing || FormEditorMode) return false;
      BoxFromTableStyle style = BoxFromTableStyle.GetCurrent();
      if (style.Name != tbStyleName.Text)
        style.Name = tbStyleName.Text;

      if (style.File != tbFile.Text)
        style.File = tbFile.Text.Trim();

      if (cbSeparator.Text.Trim() == "Tab") style.Separator = "\t";
      else if (cbSeparator.Text.Trim() == Cns.Local(BoxFromTableL.Comma)) style.Separator = ",";
      else if (cbSeparator.Text.Trim() == Cns.Local(BoxFromTableL.Semicolon)) style.Separator = ";";
      else style.Separator = cbSeparator.Text;

      style.ServerAddress = cbServerAddress.Text.Trim();
      HistoryList.SaveText(cbServerAddress);

      style.Page = tbPage.Text.Trim();
      if (cbDrill.Checked) style.Flags |= CreateBoxEnum.Drill; else style.Flags &= ~CreateBoxEnum.Drill;
      if (cbMakeBlock.Checked) style.Flags |= CreateBoxEnum.MakeBlock; else style.Flags &= ~CreateBoxEnum.MakeBlock;
      if (cbMakeGroup.Checked) style.Flags |= CreateBoxEnum.MakeGroup; else style.Flags &= ~CreateBoxEnum.MakeGroup;
      if (cbRequestFile.Checked) style.Flags |= CreateBoxEnum.RequestFile; else style.Flags &= ~CreateBoxEnum.RequestFile;
      if (cbExpose.Checked) style.Flags |= CreateBoxEnum.Expose; else style.Flags &= ~CreateBoxEnum.Expose;
      ShowData();
      return true;
    }

    public override void
    ShowData()
    {
      if (Loading || Disposing || FormEditorMode) return;
      try
      {
        if (FormEditorMode) return;
        Loading = true;
        SuspendLayout();
        BoxFromTableStyle style = BoxFromTableStyle.GetCurrent(); // важно вызвать статический конструктор стиля ДО загрузки списка стилей
        LoadStyleList();
        SelectCurrentStyle();

        tbStyleName.Text = style.Name;
        tbFile.Text = style.File;
        cbServerAddress.Text = style.ServerAddress;
        tbPage.Text = style.Page;
        if (style.Separator == "\t") cbSeparator.Text = "Tab";
        else if (style.Separator == ",") cbSeparator.Text = Cns.Local(BoxFromTableL.Comma);
        else if (style.Separator == ";") cbSeparator.Text = Cns.Local(BoxFromTableL.Semicolon);
        else cbSeparator.Text = style.Separator;
         cbDrill.Checked = (style.Flags & CreateBoxEnum.Drill) != 0;
        cbMakeBlock.Checked = (style.Flags & CreateBoxEnum.MakeBlock) != 0;
        cbMakeGroup.Checked = (style.Flags & CreateBoxEnum.MakeGroup) != 0;
        cbRequestFile.Checked = (style.Flags & CreateBoxEnum.RequestFile) != 0;
        cbExpose.Checked = (style.Flags & CreateBoxEnum.Expose) != 0;

        pnFile.Visible = !cbRequestFile.Checked;
#if VARS || DEBUG
        pnServerAddress.Visible = true;
#else
        pnServerAddress.Visible = false;
#endif
      }
      finally
      {
        ResumeLayout(true);
        Loading = false;
      }
    }

    protected override void
    Localize()
    {
      base.Localize();
      SetText(lbStyleHint, BoxFromTableL.StyleHint);
      SetTextTip(lbStyleName, tbStyleName, CmdLineL.StyleName, BoxFromTableL.StyleNameTip);
      SetTextTip(lbFile, tbFile, BoxFromTableL.File, BoxFromTableL.FileTip);
      SetTextTip(lbPage, tbPage, BoxFromTableL.Page, BoxFromTableL.PageTip);
      SetTextTip(lbSeparator, cbSeparator, BoxFromTableL.Separator, BoxFromTableL.SeparatorTip);
      SetTip(btFile, BoxFromTableL.FileTip);
      SetTextTip(lbServerAddress, cbServerAddress, BoxFromTableL.ServerAddress, BoxFromTableL.ServerAddressTip);
      SetTextTip(cbMakeBlock, BoxFromTableL.MakeBlock, BoxFromTableL.MakeBlockTip);
      SetTextTip(cbDrill, BoxFromTableL.Drill, BoxFromTableL.DrillTip);
      SetTextTip(cbMakeGroup, BoxFromTableL.MakeGroup, BoxFromTableL.MakeGroupTip);
      SetTextTip(cbRequestFile, BoxFromTableL.RequestFile, BoxFromTableL.RequestFileTip);
      SetTextTip(cbExpose, BoxFromTableL.Expose, BoxFromTableL.ExposeTip);
    }

    public override void
    SetDefault()
    {
      if (FormEditorMode || IsNullOrEmpty(RegKey)) return;
      BoxFromTableStyle style = BoxFromTableStyle.GetCurrent();
      style.SetDefault();
    }

    //internal static void
    //ShowDialog()
    //{
    //  using BoxFromTableStyle ab = new();
    //  ab.ShowDialog(ab.lbTitul.Text);
    //}

    protected void
    Editor_Validated(object sender, EventArgs e)
    {
      SaveData();
    }

    private void
    BtFile_Click(object sender, EventArgs e)
    {
      AOpenDialog dlg = new(Cns.Local(BoxFromTableL.SelectFile), tbFile.Text, "csv;tsv;xls;xlsx;xlsm;json",
          Cns.Local(BoxFromTableL.DialogTitle), AOpenDialog.OpenFileDialogFlags.AllowAnyExtension);
      if (dlg.ShowDialog() == DialogResult.OK)
      {
        tbFile.Text = dlg.Filename;
        SaveData();
      }
    }

    private void
    CbServerAddress_DropDown(object sender, EventArgs e)
    {
      cbServerAddress.Items.Clear();
      cbServerAddress.Items.Add(BoxFromTableStyle.DefServerAddress);
      HistoryList.ReadList(cbServerAddress);
    }

    private void
    CbMakeGroup_CheckedChanged(object sender, EventArgs e)
    {
      if (cbMakeGroup.Checked) cbMakeBlock.Checked = false;
    }

    private void
    CbMakeBlock_CheckedChanged(object sender, EventArgs e)
    {
      if (cbMakeBlock.Checked) cbMakeGroup.Checked = false;
    }

    private void
    CbRequestFile_CheckedChanged(object sender, EventArgs e)
    {
      pnFile.Visible = !cbRequestFile.Checked;
    }

    private void
    CbSeparator_DropDown(object sender, EventArgs e)
    {
      cbSeparator.Items.Clear();
      cbSeparator.Items.Add(Cns.Local(BoxFromTableL.Comma));
      cbSeparator.Items.Add(Cns.Local(BoxFromTableL.Semicolon));
      cbSeparator.Items.Add("Tab");
    }

  }
}
