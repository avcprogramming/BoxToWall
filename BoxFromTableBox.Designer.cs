
using System;

namespace AVC
{
  partial class BoxFromTableBox
  {
    /// <summary> 
    /// Обязательная переменная конструктора.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Освободить все используемые ресурсы.
    /// </summary>
    /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Код, автоматически созданный конструктором компонентов

    /// <summary> 
    /// Требуемый метод для поддержки конструктора — не изменяйте 
    /// содержимое этого метода с помощью редактора кода.
    /// </summary>
    private void InitializeComponent()
    {
      this.pnName = new System.Windows.Forms.Panel();
      this.tbStyleName = new System.Windows.Forms.TextBox();
      this.lbStyleName = new System.Windows.Forms.Label();
      this.pnFile = new System.Windows.Forms.Panel();
      this.tbFile = new System.Windows.Forms.TextBox();
      this.btFile = new System.Windows.Forms.Button();
      this.lbFile = new System.Windows.Forms.Label();

      this.pnPage = new System.Windows.Forms.Panel();
      this.tbPage = new System.Windows.Forms.TextBox();
      this.lbPage = new System.Windows.Forms.Label();

      this.pnSeparator = new System.Windows.Forms.Panel();
      this.cbSeparator = new FlatComboBox();
      this.lbSeparator = new System.Windows.Forms.Label();

      this.pnServerAddress = new System.Windows.Forms.Panel();
      this.cbServerAddress = new FlatComboBox();
      this.lbServerAddress = new System.Windows.Forms.Label();
      this.cbMakeBlock = new System.Windows.Forms.CheckBox();
      this.cbDrill = new System.Windows.Forms.CheckBox();
      this.cbMakeGroup = new System.Windows.Forms.CheckBox();
      this.cbRequestFile = new System.Windows.Forms.CheckBox();
      this.cbExpose = new System.Windows.Forms.CheckBox();
      this.lbStyleHint = new System.Windows.Forms.Label();

      this.pnBlockLayer = new System.Windows.Forms.Panel();
      this.cbBlockLayer = new FlatComboBox();
      this.lbBlockLayer = new System.Windows.Forms.Label();

      this.pnStyle.SuspendLayout();
      this.pnHeader.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.errProvider)).BeginInit();
      this.pnName.SuspendLayout();
      this.pnFile.SuspendLayout();
      this.pnServerAddress.SuspendLayout();
      this.pnPage.SuspendLayout();
      this.pnBlockLayer.SuspendLayout();
      this.pnSeparator.SuspendLayout();
      this.SuspendLayout();
      // 
      // lbTitle
      // 
      this.lbTitle.Text = "Detail Drawings";
      // 
      // pnName
      // 
      this.pnName.Controls.Add(this.tbStyleName);
      this.pnName.Controls.Add(this.lbStyleName);
      this.pnName.Dock = System.Windows.Forms.DockStyle.Top;
      this.pnName.Location = new System.Drawing.Point(0, 255);
      this.pnName.Name = "pnName";
      this.pnName.Padding = new System.Windows.Forms.Padding(3);
      this.pnName.Size = new System.Drawing.Size(150, 26);
      this.pnName.TabIndex = 2;
      // 
      // tbStyleName
      // 
      this.tbStyleName.BackColor = System.Drawing.SystemColors.Control;
      this.tbStyleName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.tbStyleName.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tbStyleName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
      this.tbStyleName.ForeColor = System.Drawing.SystemColors.GrayText;
      this.tbStyleName.Location = new System.Drawing.Point(64, 3);
      this.tbStyleName.Name = "tbStyleName";
      this.tbStyleName.Size = new System.Drawing.Size(83, 20);
      this.tbStyleName.TabIndex = 1;
      this.toolTip.SetToolTip(this.tbStyleName, "The name for this DimDet style. \r\nNot used in the program. Only for convenience o" +
        "f choice.");
      this.tbStyleName.Validated += new System.EventHandler(this.Editor_Validated);
      // 
      // lbStyleName
      // 
      this.lbStyleName.AutoSize = true;
      this.lbStyleName.Dock = System.Windows.Forms.DockStyle.Left;
      this.lbStyleName.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.lbStyleName.Location = new System.Drawing.Point(3, 3);
      this.lbStyleName.Name = "lbStyleName";
      this.lbStyleName.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
      this.lbStyleName.Size = new System.Drawing.Size(61, 16);
      this.lbStyleName.TabIndex = 0;
      this.lbStyleName.Text = "Style Name";
      // 
      // pnFile
      // 
      this.pnFile.Controls.Add(this.tbFile);
      this.pnFile.Controls.Add(this.btFile);
      this.pnFile.Controls.Add(this.lbFile);
      this.pnFile.Dock = System.Windows.Forms.DockStyle.Top;
      this.pnFile.Location = new System.Drawing.Point(0, 281);
      this.pnFile.Name = "pnFile";
      this.pnFile.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
      this.pnFile.Size = new System.Drawing.Size(150, 23);
      this.pnFile.TabIndex = 3;
      // 
      // tbFile
      // 
      this.tbFile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.tbFile.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tbFile.Location = new System.Drawing.Point(96, 0);
      this.tbFile.Name = "tbFile";
      this.tbFile.Size = new System.Drawing.Size(27, 20);
      this.tbFile.TabIndex = 1;
      this.tbFile.Validated += new System.EventHandler(this.Editor_Validated);
      // 
      // btFile
      // 
      this.btFile.AutoSize = true;
      this.btFile.Dock = System.Windows.Forms.DockStyle.Right;
      this.btFile.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.btFile.Image = global::AVC.Common.OpenFile16;
      this.btFile.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.btFile.Location = new System.Drawing.Point(123, 0);
      this.btFile.Name = "btFile";
      this.btFile.Size = new System.Drawing.Size(24, 23);
      this.btFile.TabIndex = 2;
      this.btFile.TextImageRelation = System.Windows.Forms.TextImageRelation.TextAboveImage;
      this.btFile.UseVisualStyleBackColor = true;
      this.btFile.Click += new System.EventHandler(this.BtFile_Click);
      // 
      // lbFile
      // 
      this.lbFile.AutoSize = true;
      this.lbFile.Dock = System.Windows.Forms.DockStyle.Left;
      this.lbFile.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.lbFile.Location = new System.Drawing.Point(3, 0);
      this.lbFile.Name = "lbFile";
      this.lbFile.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
      this.lbFile.Size = new System.Drawing.Size(93, 16);
      this.lbFile.TabIndex = 0;
      this.lbFile.Text = "File";

      // 
      // pnPage
      // 
      this.pnPage.Controls.Add(this.tbPage);
      this.pnPage.Controls.Add(this.lbPage);
      this.pnPage.Dock = System.Windows.Forms.DockStyle.Top;
      this.pnPage.Location = new System.Drawing.Point(0, 281);
      this.pnPage.Name = "pnPage";
      this.pnPage.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
      this.pnPage.Size = new System.Drawing.Size(150, 23);
      this.pnPage.TabIndex = 4;
      // 
      // tbPage
      // 
      this.tbPage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
      this.tbPage.Dock = System.Windows.Forms.DockStyle.Fill;
      this.tbPage.Location = new System.Drawing.Point(96, 0);
      this.tbPage.Name = "tbPage";
      this.tbPage.Size = new System.Drawing.Size(27, 20);
      this.tbPage.TabIndex = 1;
      this.tbPage.Validated += new System.EventHandler(this.Editor_Validated);
      // 
      // lbPage
      // 
      this.lbPage.AutoSize = true;
      this.lbPage.Dock = System.Windows.Forms.DockStyle.Left;
      this.lbPage.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.lbPage.Location = new System.Drawing.Point(3, 0);
      this.lbPage.Name = "lbPage";
      this.lbPage.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
      this.lbPage.Size = new System.Drawing.Size(93, 16);
      this.lbPage.TabIndex = 0;
      this.lbPage.Text = "Page";

      // 
      // pnSeparator
      // 
      this.pnSeparator.Controls.Add(this.cbSeparator);
      this.pnSeparator.Controls.Add(this.lbSeparator);
      this.pnSeparator.Dock = System.Windows.Forms.DockStyle.Top;
      this.pnSeparator.Location = new System.Drawing.Point(0, 281);
      this.pnSeparator.Name = "pnSeparator";
      this.pnSeparator.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
      this.pnSeparator.Size = new System.Drawing.Size(150, 23);
      this.pnSeparator.TabIndex = 5;
      // 
      // lbSeparator
      // 
      this.lbSeparator.AutoSize = true;
      this.lbSeparator.Dock = System.Windows.Forms.DockStyle.Left;
      this.lbSeparator.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.lbSeparator.Location = new System.Drawing.Point(3, 0);
      this.lbSeparator.Name = "lbSeparator";
      this.lbSeparator.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
      this.lbSeparator.Size = new System.Drawing.Size(93, 16);
      this.lbSeparator.TabIndex = 0;
      this.lbSeparator.Text = "Separator";
      // 
      // cbSeparator
      // 
      this.cbSeparator.BackColor = System.Drawing.SystemColors.Control;
      this.cbSeparator.Dock = System.Windows.Forms.DockStyle.Fill;
      this.cbSeparator.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.cbSeparator.Location = new System.Drawing.Point(89, 3);
      this.cbSeparator.Name = "cbSeparator";
      this.cbSeparator.Size = new System.Drawing.Size(58, 21);
      this.cbSeparator.TabIndex = 1;
      this.cbSeparator.DropDown += new System.EventHandler(this.CbSeparator_DropDown);
      this.cbSeparator.Validated += new System.EventHandler(this.Editor_Validated);

      // 
      // pnServerAddress
      // 
      this.pnServerAddress.Controls.Add(this.cbServerAddress);
      this.pnServerAddress.Controls.Add(this.lbServerAddress);
      this.pnServerAddress.Dock = System.Windows.Forms.DockStyle.Top;
      this.pnServerAddress.Location = new System.Drawing.Point(0, 304);
      this.pnServerAddress.Name = "pnServerAddress";
      this.pnServerAddress.Padding = new System.Windows.Forms.Padding(3);
      this.pnServerAddress.Size = new System.Drawing.Size(150, 26);
      this.pnServerAddress.TabIndex = 6;
      // 
      // cbServerAddress
      // 
      this.cbServerAddress.BackColor = System.Drawing.SystemColors.Control;
      this.cbServerAddress.Dock = System.Windows.Forms.DockStyle.Fill;
      this.cbServerAddress.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.cbServerAddress.Location = new System.Drawing.Point(89, 3);
      this.cbServerAddress.Name = "cbServerAddress";
      this.cbServerAddress.Size = new System.Drawing.Size(58, 21);
      this.cbServerAddress.TabIndex = 1;
      this.cbServerAddress.DropDown += new System.EventHandler(this.CbServerAddress_DropDown);
      this.cbServerAddress.Validated += new System.EventHandler(this.Editor_Validated);
      // 
      // lbServerAddress
      // 
      this.lbServerAddress.AutoSize = true;
      this.lbServerAddress.Dock = System.Windows.Forms.DockStyle.Left;
      this.lbServerAddress.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.lbServerAddress.Location = new System.Drawing.Point(3, 3);
      this.lbServerAddress.Name = "lbServerAddress";
      this.lbServerAddress.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
      this.lbServerAddress.Size = new System.Drawing.Size(86, 16);
      this.lbServerAddress.TabIndex = 0;
      this.lbServerAddress.Text = "Server Address";
      // 
      // cbRequestFile
      // 
      this.cbRequestFile.AutoSize = true;
      this.cbRequestFile.Dock = System.Windows.Forms.DockStyle.Top;
      this.cbRequestFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
      this.cbRequestFile.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.cbRequestFile.Location = new System.Drawing.Point(0, 424);
      this.cbRequestFile.Name = "cbRequestFile";
      this.cbRequestFile.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
      this.cbRequestFile.Size = new System.Drawing.Size(150, 17);
      this.cbRequestFile.TabIndex = 9;
      this.cbRequestFile.Text = "Request File";
      this.cbRequestFile.UseVisualStyleBackColor = true;
      this.cbRequestFile.CheckedChanged += CbRequestFile_CheckedChanged;
      this.cbRequestFile.Validated += new System.EventHandler(this.Editor_Validated);
      // 
      // cbMakeBlock
      // 
      this.cbMakeBlock.AutoSize = true;
      this.cbMakeBlock.Dock = System.Windows.Forms.DockStyle.Top;
      this.cbMakeBlock.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
      this.cbMakeBlock.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.cbMakeBlock.Location = new System.Drawing.Point(0, 390);
      this.cbMakeBlock.Name = "cbMakeBlock";
      this.cbMakeBlock.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
      this.cbMakeBlock.Size = new System.Drawing.Size(150, 17);
      this.cbMakeBlock.TabIndex = 8;
      this.cbMakeBlock.Text = "Make Block";
      this.cbMakeBlock.UseVisualStyleBackColor = true;
      this.cbMakeBlock.CheckedChanged += new System.EventHandler(this.CbMakeBlock_CheckedChanged);
      this.cbMakeBlock.Validated += new System.EventHandler(this.Editor_Validated);

      // 
      // pnBlockLayer
      // 
      this.pnBlockLayer.Controls.Add(this.cbBlockLayer);
      this.pnBlockLayer.Controls.Add(this.lbBlockLayer);
      this.pnBlockLayer.Dock = System.Windows.Forms.DockStyle.Top;
      this.pnBlockLayer.Location = new System.Drawing.Point(0, 281);
      this.pnBlockLayer.Name = "pnBlockLayer";
      this.pnBlockLayer.Padding = new System.Windows.Forms.Padding(10, 0, 3, 0);
      this.pnBlockLayer.Size = new System.Drawing.Size(150, 23);
      this.pnBlockLayer.TabIndex = 9;
      // 
      // cbBlockLayer
      // 
      this.cbBlockLayer.BackColor = System.Drawing.SystemColors.Control;
      this.cbBlockLayer.Dock = System.Windows.Forms.DockStyle.Left;
      this.cbBlockLayer.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
      this.cbBlockLayer.Location = new System.Drawing.Point(63, 6);
      this.cbBlockLayer.Margin = new System.Windows.Forms.Padding(2);
      this.cbBlockLayer.Name = "cbBlockLayer";
      this.cbBlockLayer.Size = new System.Drawing.Size(153, 21);
      this.cbBlockLayer.Sorted = true;
      this.cbBlockLayer.TabIndex = 1;
      this.cbBlockLayer.DropDown += new System.EventHandler(this.CbBlockLayer_DropDown);
      this.cbBlockLayer.Validated += new System.EventHandler(this.Editor_Validated);
      // 
      // lbBlockLayer
      // 
      this.lbBlockLayer.AutoSize = true;
      this.lbBlockLayer.Dock = System.Windows.Forms.DockStyle.Left;
      this.lbBlockLayer.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.lbBlockLayer.Location = new System.Drawing.Point(3, 0);
      this.lbBlockLayer.Name = "lbBlockLayer";
      this.lbBlockLayer.Padding = new System.Windows.Forms.Padding(0, 3, 0, 0);
      this.lbBlockLayer.Size = new System.Drawing.Size(93, 16);
      this.lbBlockLayer.TabIndex = 0;
      this.lbBlockLayer.Text = "Wall Block Layer";

      // 
      // cbMakeGroup
      // 
      this.cbMakeGroup.AutoSize = true;
      this.cbMakeGroup.Dock = System.Windows.Forms.DockStyle.Top;
      this.cbMakeGroup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
      this.cbMakeGroup.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.cbMakeGroup.Location = new System.Drawing.Point(0, 424);
      this.cbMakeGroup.Name = "cbMakeGroup";
      this.cbMakeGroup.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
      this.cbMakeGroup.Size = new System.Drawing.Size(150, 17);
      this.cbMakeGroup.TabIndex = 10;
      this.cbMakeGroup.Text = "Make Group";
      this.cbMakeGroup.UseVisualStyleBackColor = true;
      this.cbMakeGroup.CheckedChanged += new System.EventHandler(this.CbMakeGroup_CheckedChanged);
      this.cbMakeGroup.Validated += new System.EventHandler(this.Editor_Validated);
      // 
      // cbDrill
      // 
      this.cbDrill.AutoSize = true;
      this.cbDrill.Dock = System.Windows.Forms.DockStyle.Top;
      this.cbDrill.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
      this.cbDrill.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.cbDrill.Location = new System.Drawing.Point(0, 407);
      this.cbDrill.Name = "cbDrill";
      this.cbDrill.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
      this.cbDrill.Size = new System.Drawing.Size(150, 17);
      this.cbDrill.TabIndex = 11;
      this.cbDrill.Text = "Drill";
      this.cbDrill.UseVisualStyleBackColor = true;
      this.cbDrill.Validated += new System.EventHandler(this.Editor_Validated);
      // 
      // cbExpose
      // 
      this.cbExpose.AutoSize = true;
      this.cbExpose.Dock = System.Windows.Forms.DockStyle.Top;
      this.cbExpose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
      this.cbExpose.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.cbExpose.Location = new System.Drawing.Point(0, 407);
      this.cbExpose.Name = "cbExpose";
      this.cbExpose.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
      this.cbExpose.Size = new System.Drawing.Size(150, 17);
      this.cbExpose.TabIndex = 12;
      this.cbExpose.Text = "Expose assemblies";
      this.cbExpose.UseVisualStyleBackColor = true;
      this.cbExpose.Validated += new System.EventHandler(this.Editor_Validated);

      // 
      // lbStyleHint
      // 
      this.lbStyleHint.Dock = System.Windows.Forms.DockStyle.Top;
      this.lbStyleHint.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
      this.lbStyleHint.ForeColor = System.Drawing.SystemColors.ControlText;
      this.lbStyleHint.ImeMode = System.Windows.Forms.ImeMode.NoControl;
      this.lbStyleHint.Location = new System.Drawing.Point(0, 32);
      this.lbStyleHint.Name = "lbStyleHint";
      this.lbStyleHint.Padding = new System.Windows.Forms.Padding(10, 5, 0, 10);
      this.lbStyleHint.Size = new System.Drawing.Size(150, 223);
      this.lbStyleHint.TabIndex = 1;
      this.lbStyleHint.Text = "StyleHint";
      this.lbStyleHint.TextAlign = System.Drawing.ContentAlignment.TopLeft;
      // 
      // BoxFromTableBox
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.lbStyleHint);
      this.Controls.Add(this.cbExpose);
      this.Controls.Add(this.cbDrill);
      this.Controls.Add(this.pnBlockLayer);
      this.Controls.Add(this.cbMakeBlock);
      this.Controls.Add(this.cbMakeGroup);
      this.Controls.Add(this.pnServerAddress);
      this.Controls.Add(this.pnSeparator);
      this.Controls.Add(this.pnPage);
      this.Controls.Add(this.pnFile);
      this.Controls.Add(this.cbRequestFile);
      this.Controls.Add(this.pnName);
      this.MinimumSize = new System.Drawing.Size(150, 0);
      this.Name = "BoxFromTableBox";
      this.Size = new System.Drawing.Size(150, 458);
      this.Controls.SetChildIndex(this.pnHeader, 0);
      this.Controls.SetChildIndex(this.lbIdeas, 0);
      this.Controls.SetChildIndex(this.lbIdeasTip, 0);
      this.Controls.SetChildIndex(this.pnName, 0);
      this.Controls.SetChildIndex(this.cbRequestFile, 0);
      this.Controls.SetChildIndex(this.pnFile, 0);
      this.Controls.SetChildIndex(this.pnPage, 0);
      this.Controls.SetChildIndex(this.pnSeparator, 0);
      this.Controls.SetChildIndex(this.pnServerAddress, 0);
      this.Controls.SetChildIndex(this.cbMakeBlock, 0);
      this.Controls.SetChildIndex(this.pnBlockLayer, 0);
      this.Controls.SetChildIndex(this.cbMakeGroup, 0);
      this.Controls.SetChildIndex(this.cbDrill, 0);
      this.Controls.SetChildIndex(this.cbExpose, 0);
      this.Controls.SetChildIndex(this.lbStyleHint, 0);
      this.pnStyle.ResumeLayout(false);
      this.pnHeader.ResumeLayout(false);
      this.pnHeader.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)(this.errProvider)).EndInit();
      this.pnName.ResumeLayout(false);
      this.pnName.PerformLayout();
      this.pnFile.ResumeLayout(false);
      this.pnFile.PerformLayout();
      this.pnServerAddress.ResumeLayout(false);
      this.pnServerAddress.PerformLayout();
      this.pnPage.ResumeLayout(false);
      this.pnPage.PerformLayout();
      this.pnSeparator.ResumeLayout(false);
      this.pnSeparator.PerformLayout();
      this.pnBlockLayer.ResumeLayout(false);
      this.pnBlockLayer.PerformLayout();
      this.ResumeLayout(false);
      this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Panel pnName;
    private System.Windows.Forms.TextBox tbStyleName;
    private System.Windows.Forms.Label lbStyleName;

    private System.Windows.Forms.Panel pnFile;
    private System.Windows.Forms.TextBox tbFile;
    private System.Windows.Forms.Label lbFile;
    private System.Windows.Forms.Button btFile;

    private System.Windows.Forms.Panel pnPage;
    private System.Windows.Forms.TextBox tbPage;
    private System.Windows.Forms.Label lbPage;

    private System.Windows.Forms.Panel pnSeparator;
    private FlatComboBox cbSeparator;
    private System.Windows.Forms.Label lbSeparator;

    private System.Windows.Forms.Panel pnServerAddress;
    private FlatComboBox cbServerAddress;
    private System.Windows.Forms.Label lbServerAddress;
    private System.Windows.Forms.CheckBox cbMakeBlock;
    private System.Windows.Forms.CheckBox cbDrill;
    private System.Windows.Forms.CheckBox cbMakeGroup;
    private System.Windows.Forms.CheckBox cbRequestFile;
    private System.Windows.Forms.CheckBox cbExpose;
    private System.Windows.Forms.Label lbStyleHint;

    private System.Windows.Forms.Panel pnBlockLayer;
    private FlatComboBox cbBlockLayer;
    private System.Windows.Forms.Label lbBlockLayer;
  }
}
