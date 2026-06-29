namespace StrangeLens
{
   partial class SettingsForm
   {
      private System.ComponentModel.IContainer components = null;

      protected override void Dispose(bool disposing)
      {
         if (disposing)
         {
            this.textFont?.Dispose();
            if (components != null) components.Dispose();
         }
         base.Dispose(disposing);
      }

      private void InitializeComponent()
      {
         this.components = new System.ComponentModel.Container();
         var resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));

         this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
         this.contextMenu = new System.Windows.Forms.ContextMenuStrip();
         this.colorGrid = new System.Windows.Forms.ColorDialog();
         this.SuspendLayout();

         this.notifyIcon.Text = "Lens";
         this.notifyIcon.Visible = true;
         this.notifyIcon.MouseClick       += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseClick);
         this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseClick);

         this.colorGrid.AnyColor      = true;
         this.colorGrid.FullOpen      = true;
         this.colorGrid.SolidColorOnly = true;

         this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
         this.AutoScaleMode   = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize      = new System.Drawing.Size(320, 100); // BuildLayout sets final height
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
         this.Icon            = (System.Drawing.Icon)resources.GetObject("$this.Icon");
         this.MaximizeBox     = false;
         this.MinimizeBox     = false;
         this.Name            = "SettingsForm";
         this.Text            = "Lens Settings";
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SettingsForm_FormClosing);
         this.ResumeLayout(false);
      }

      private System.Windows.Forms.NotifyIcon      notifyIcon;
      private System.Windows.Forms.ContextMenuStrip contextMenu;
      private System.Windows.Forms.ColorDialog     colorGrid;
   }
}
