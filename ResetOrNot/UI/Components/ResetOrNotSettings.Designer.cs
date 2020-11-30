namespace ResetOrNot.UI.Components
{
    partial class ResetOrNotSettings
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.RecentLabel = new System.Windows.Forms.Label();
            this.AttemptCountBox = new System.Windows.Forms.NumericUpDown();
            this.CreditsLabel = new System.Windows.Forms.Label();
            this.TimeToResetCountBox = new System.Windows.Forms.NumericUpDown();
            this.ResetTimeLabel = new System.Windows.Forms.Label();
            this.SecondsLabel = new System.Windows.Forms.Label();
            this.AttemptsLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.AttemptCountBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TimeToResetCountBox)).BeginInit();
            this.SuspendLayout();
            // 
            // RecentLabel
            // 
            this.RecentLabel.AutoSize = true;
            this.RecentLabel.Location = new System.Drawing.Point(3, 41);
            this.RecentLabel.Name = "RecentLabel";
            this.RecentLabel.Size = new System.Drawing.Size(87, 13);
            this.RecentLabel.TabIndex = 1;
            this.RecentLabel.Text = "Use most recent:";
            // 
            // AttemptCountBox
            // 
            this.AttemptCountBox.Location = new System.Drawing.Point(127, 39);
            this.AttemptCountBox.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
            this.AttemptCountBox.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.AttemptCountBox.Name = "AttemptCountBox";
            this.AttemptCountBox.Size = new System.Drawing.Size(51, 20);
            this.AttemptCountBox.TabIndex = 1;
            this.AttemptCountBox.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // CreditsLabel
            // 
            this.CreditsLabel.AutoSize = true;
            this.CreditsLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CreditsLabel.Location = new System.Drawing.Point(3, 13);
            this.CreditsLabel.Name = "CreditsLabel";
            this.CreditsLabel.Size = new System.Drawing.Size(371, 13);
            this.CreditsLabel.TabIndex = 4;
            this.CreditsLabel.Text = "ResetOrNot by gottagofaster (based on PBChance by SethBling)";
            // 
            // TimeToResetCountBox
            // 
            this.TimeToResetCountBox.Location = new System.Drawing.Point(127, 69);
            this.TimeToResetCountBox.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
            this.TimeToResetCountBox.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.TimeToResetCountBox.Name = "TimeToResetCountBox";
            this.TimeToResetCountBox.Size = new System.Drawing.Size(51, 20);
            this.TimeToResetCountBox.TabIndex = 7;
            this.TimeToResetCountBox.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // ResetTimeLabel
            // 
            this.ResetTimeLabel.AutoSize = true;
            this.ResetTimeLabel.Location = new System.Drawing.Point(3, 71);
            this.ResetTimeLabel.Name = "ResetTimeLabel";
            this.ResetTimeLabel.Size = new System.Drawing.Size(118, 13);
            this.ResetTimeLabel.TabIndex = 8;
            this.ResetTimeLabel.Text = "Time to reset the game:";
            // 
            // SecondsLabel
            // 
            this.SecondsLabel.AutoSize = true;
            this.SecondsLabel.Location = new System.Drawing.Point(184, 71);
            this.SecondsLabel.Name = "SecondsLabel";
            this.SecondsLabel.Size = new System.Drawing.Size(47, 13);
            this.SecondsLabel.TabIndex = 9;
            this.SecondsLabel.Text = "seconds";
            // 
            // AttemptsLabel
            // 
            this.AttemptsLabel.AutoSize = true;
            this.AttemptsLabel.Location = new System.Drawing.Point(184, 41);
            this.AttemptsLabel.Name = "AttemptsLabel";
            this.AttemptsLabel.Size = new System.Drawing.Size(47, 13);
            this.AttemptsLabel.TabIndex = 10;
            this.AttemptsLabel.Text = "attempts";
            // 
            // ResetOrNotSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.AttemptsLabel);
            this.Controls.Add(this.SecondsLabel);
            this.Controls.Add(this.TimeToResetCountBox);
            this.Controls.Add(this.ResetTimeLabel);
            this.Controls.Add(this.CreditsLabel);
            this.Controls.Add(this.AttemptCountBox);
            this.Controls.Add(this.RecentLabel);
            this.Name = "ResetOrNotSettings";
            this.Size = new System.Drawing.Size(373, 98);
            ((System.ComponentModel.ISupportInitialize)(this.AttemptCountBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TimeToResetCountBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label RecentLabel;
        private System.Windows.Forms.NumericUpDown AttemptCountBox;
        private System.Windows.Forms.Label CreditsLabel;
        private System.Windows.Forms.NumericUpDown TimeToResetCountBox;
        private System.Windows.Forms.Label ResetTimeLabel;
        private System.Windows.Forms.Label SecondsLabel;
        private System.Windows.Forms.Label AttemptsLabel;
    }
}
