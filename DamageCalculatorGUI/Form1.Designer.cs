namespace DamageCalculatorGUI
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.CalculateDamageStatsButton = new System.Windows.Forms.Button();
            this.AverageDamageEncounterLabel = new System.Windows.Forms.Label();
            this.AverageDamageEncounterTextbox = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.AverageAccuracyLabel = new System.Windows.Forms.Label();
            this.AverageDamageAttackLabel = new System.Windows.Forms.Label();
            this.AverageAccuracyTextbox = new System.Windows.Forms.TextBox();
            this.AverageDamageRoundLabel = new System.Windows.Forms.Label();
            this.AverageDamageAttackTextbox = new System.Windows.Forms.TextBox();
            this.AverageDamageRoundTextbox = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.SettingsGroupBox = new System.Windows.Forms.GroupBox();
            this.DamageDiceGroupBox = new System.Windows.Forms.GroupBox();
            this.DamageDiceEditButton = new System.Windows.Forms.Button();
            this.DamageDiceDeleteButton = new System.Windows.Forms.Button();
            this.DamageDiceAddButton = new System.Windows.Forms.Button();
            this.DamageDiceTextBox = new System.Windows.Forms.TextBox();
            this.DamageDiceListBox = new System.Windows.Forms.ListBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SettingsGroupBox.SuspendLayout();
            this.DamageDiceGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // CalculateDamageStatsButton
            // 
            this.CalculateDamageStatsButton.Location = new System.Drawing.Point(675, 406);
            this.CalculateDamageStatsButton.Name = "CalculateDamageStatsButton";
            this.CalculateDamageStatsButton.Size = new System.Drawing.Size(113, 35);
            this.CalculateDamageStatsButton.TabIndex = 0;
            this.CalculateDamageStatsButton.Text = "Calculate Stats";
            this.CalculateDamageStatsButton.UseVisualStyleBackColor = true;
            this.CalculateDamageStatsButton.Click += new System.EventHandler(this.CalculateDamageStatsButton_Click);
            // 
            // AverageDamageEncounterLabel
            // 
            this.AverageDamageEncounterLabel.AutoSize = true;
            this.AverageDamageEncounterLabel.Location = new System.Drawing.Point(6, 19);
            this.AverageDamageEncounterLabel.Name = "AverageDamageEncounterLabel";
            this.AverageDamageEncounterLabel.Size = new System.Drawing.Size(108, 15);
            this.AverageDamageEncounterLabel.TabIndex = 2;
            this.AverageDamageEncounterLabel.Text = "Encounter Damage";
            // 
            // AverageDamageEncounterTextbox
            // 
            this.AverageDamageEncounterTextbox.Location = new System.Drawing.Point(6, 37);
            this.AverageDamageEncounterTextbox.Name = "AverageDamageEncounterTextbox";
            this.AverageDamageEncounterTextbox.ReadOnly = true;
            this.AverageDamageEncounterTextbox.Size = new System.Drawing.Size(162, 23);
            this.AverageDamageEncounterTextbox.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.AverageAccuracyLabel);
            this.groupBox1.Controls.Add(this.AverageDamageAttackLabel);
            this.groupBox1.Controls.Add(this.AverageAccuracyTextbox);
            this.groupBox1.Controls.Add(this.AverageDamageRoundLabel);
            this.groupBox1.Controls.Add(this.AverageDamageAttackTextbox);
            this.groupBox1.Controls.Add(this.AverageDamageEncounterLabel);
            this.groupBox1.Controls.Add(this.AverageDamageRoundTextbox);
            this.groupBox1.Controls.Add(this.AverageDamageEncounterTextbox);
            this.groupBox1.Location = new System.Drawing.Point(612, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(176, 212);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Average";
            // 
            // AverageAccuracyLabel
            // 
            this.AverageAccuracyLabel.AutoSize = true;
            this.AverageAccuracyLabel.Location = new System.Drawing.Point(6, 151);
            this.AverageAccuracyLabel.Name = "AverageAccuracyLabel";
            this.AverageAccuracyLabel.Size = new System.Drawing.Size(56, 15);
            this.AverageAccuracyLabel.TabIndex = 2;
            this.AverageAccuracyLabel.Text = "Accuracy";
            // 
            // AverageDamageAttackLabel
            // 
            this.AverageDamageAttackLabel.AutoSize = true;
            this.AverageDamageAttackLabel.Location = new System.Drawing.Point(6, 107);
            this.AverageDamageAttackLabel.Name = "AverageDamageAttackLabel";
            this.AverageDamageAttackLabel.Size = new System.Drawing.Size(88, 15);
            this.AverageDamageAttackLabel.TabIndex = 2;
            this.AverageDamageAttackLabel.Text = "Attack Damage";
            // 
            // AverageAccuracyTextbox
            // 
            this.AverageAccuracyTextbox.Location = new System.Drawing.Point(6, 169);
            this.AverageAccuracyTextbox.Name = "AverageAccuracyTextbox";
            this.AverageAccuracyTextbox.ReadOnly = true;
            this.AverageAccuracyTextbox.Size = new System.Drawing.Size(162, 23);
            this.AverageAccuracyTextbox.TabIndex = 1;
            // 
            // AverageDamageRoundLabel
            // 
            this.AverageDamageRoundLabel.AutoSize = true;
            this.AverageDamageRoundLabel.Location = new System.Drawing.Point(6, 63);
            this.AverageDamageRoundLabel.Name = "AverageDamageRoundLabel";
            this.AverageDamageRoundLabel.Size = new System.Drawing.Size(89, 15);
            this.AverageDamageRoundLabel.TabIndex = 2;
            this.AverageDamageRoundLabel.Text = "Round Damage";
            // 
            // AverageDamageAttackTextbox
            // 
            this.AverageDamageAttackTextbox.Location = new System.Drawing.Point(6, 125);
            this.AverageDamageAttackTextbox.Name = "AverageDamageAttackTextbox";
            this.AverageDamageAttackTextbox.ReadOnly = true;
            this.AverageDamageAttackTextbox.Size = new System.Drawing.Size(162, 23);
            this.AverageDamageAttackTextbox.TabIndex = 1;
            // 
            // AverageDamageRoundTextbox
            // 
            this.AverageDamageRoundTextbox.Location = new System.Drawing.Point(6, 81);
            this.AverageDamageRoundTextbox.Name = "AverageDamageRoundTextbox";
            this.AverageDamageRoundTextbox.ReadOnly = true;
            this.AverageDamageRoundTextbox.Size = new System.Drawing.Size(162, 23);
            this.AverageDamageRoundTextbox.TabIndex = 1;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.textBox2);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.textBox3);
            this.groupBox2.Controls.Add(this.textBox4);
            this.groupBox2.Location = new System.Drawing.Point(612, 230);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(176, 166);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Total";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 107);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(31, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "Crits";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 63);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(47, 15);
            this.label3.TabIndex = 2;
            this.label3.Text = "TTK 120";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(6, 125);
            this.textBox2.Name = "textBox2";
            this.textBox2.ReadOnly = true;
            this.textBox2.Size = new System.Drawing.Size(162, 23);
            this.textBox2.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 19);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(57, 15);
            this.label4.TabIndex = 2;
            this.label4.Text = "TTK 50HP";
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(6, 81);
            this.textBox3.Name = "textBox3";
            this.textBox3.ReadOnly = true;
            this.textBox3.Size = new System.Drawing.Size(162, 23);
            this.textBox3.TabIndex = 1;
            // 
            // textBox4
            // 
            this.textBox4.Location = new System.Drawing.Point(6, 37);
            this.textBox4.Name = "textBox4";
            this.textBox4.ReadOnly = true;
            this.textBox4.Size = new System.Drawing.Size(162, 23);
            this.textBox4.TabIndex = 1;
            // 
            // SettingsGroupBox
            // 
            this.SettingsGroupBox.Controls.Add(this.DamageDiceGroupBox);
            this.SettingsGroupBox.Location = new System.Drawing.Point(12, 12);
            this.SettingsGroupBox.Name = "SettingsGroupBox";
            this.SettingsGroupBox.Size = new System.Drawing.Size(594, 384);
            this.SettingsGroupBox.TabIndex = 4;
            this.SettingsGroupBox.TabStop = false;
            this.SettingsGroupBox.Text = "Settings";
            // 
            // DamageDiceGroupBox
            // 
            this.DamageDiceGroupBox.Controls.Add(this.DamageDiceEditButton);
            this.DamageDiceGroupBox.Controls.Add(this.DamageDiceDeleteButton);
            this.DamageDiceGroupBox.Controls.Add(this.DamageDiceAddButton);
            this.DamageDiceGroupBox.Controls.Add(this.DamageDiceTextBox);
            this.DamageDiceGroupBox.Controls.Add(this.DamageDiceListBox);
            this.DamageDiceGroupBox.Location = new System.Drawing.Point(6, 19);
            this.DamageDiceGroupBox.Name = "DamageDiceGroupBox";
            this.DamageDiceGroupBox.Size = new System.Drawing.Size(248, 155);
            this.DamageDiceGroupBox.TabIndex = 0;
            this.DamageDiceGroupBox.TabStop = false;
            this.DamageDiceGroupBox.Text = "Damage Dice";
            this.DamageDiceGroupBox.Leave += new System.EventHandler(this.DamageDiceGroupBox_Leave);
            // 
            // DamageDiceEditButton
            // 
            this.DamageDiceEditButton.Location = new System.Drawing.Point(133, 77);
            this.DamageDiceEditButton.Name = "DamageDiceEditButton";
            this.DamageDiceEditButton.Size = new System.Drawing.Size(109, 23);
            this.DamageDiceEditButton.TabIndex = 4;
            this.DamageDiceEditButton.Text = "Edit Die";
            this.DamageDiceEditButton.UseVisualStyleBackColor = true;
            // 
            // DamageDiceDeleteButton
            // 
            this.DamageDiceDeleteButton.Location = new System.Drawing.Point(133, 102);
            this.DamageDiceDeleteButton.Name = "DamageDiceDeleteButton";
            this.DamageDiceDeleteButton.Size = new System.Drawing.Size(109, 23);
            this.DamageDiceDeleteButton.TabIndex = 3;
            this.DamageDiceDeleteButton.Text = "Remove Die";
            this.DamageDiceDeleteButton.UseVisualStyleBackColor = true;
            // 
            // DamageDiceAddButton
            // 
            this.DamageDiceAddButton.Location = new System.Drawing.Point(133, 51);
            this.DamageDiceAddButton.Name = "DamageDiceAddButton";
            this.DamageDiceAddButton.Size = new System.Drawing.Size(109, 23);
            this.DamageDiceAddButton.TabIndex = 2;
            this.DamageDiceAddButton.Text = "Add Die";
            this.DamageDiceAddButton.UseVisualStyleBackColor = true;
            this.DamageDiceAddButton.Click += new System.EventHandler(this.DamageDiceAddButton_Click);
            // 
            // DamageDiceTextBox
            // 
            this.DamageDiceTextBox.Location = new System.Drawing.Point(133, 22);
            this.DamageDiceTextBox.Name = "DamageDiceTextBox";
            this.DamageDiceTextBox.Size = new System.Drawing.Size(109, 23);
            this.DamageDiceTextBox.TabIndex = 1;
            this.DamageDiceTextBox.Enter += new System.EventHandler(this.DamageDiceTextBox_Enter);
            // 
            // DamageDiceListBox
            // 
            this.DamageDiceListBox.FormattingEnabled = true;
            this.DamageDiceListBox.ItemHeight = 15;
            this.DamageDiceListBox.Items.AddRange(new object[] {
            "1D4+1 (2D4+2)",
            "2D2"});
            this.DamageDiceListBox.Location = new System.Drawing.Point(6, 22);
            this.DamageDiceListBox.Name = "DamageDiceListBox";
            this.DamageDiceListBox.Size = new System.Drawing.Size(121, 109);
            this.DamageDiceListBox.TabIndex = 0;
            this.DamageDiceListBox.SelectedIndexChanged += new System.EventHandler(this.DamageDiceListBox_SelectedIndexChanged);
            this.DamageDiceListBox.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.DamageDiceListBox_MouseDoubleClick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 453);
            this.Controls.Add(this.SettingsGroupBox);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.CalculateDamageStatsButton);
            this.Name = "Form1";
            this.Text = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.SettingsGroupBox.ResumeLayout(false);
            this.DamageDiceGroupBox.ResumeLayout(false);
            this.DamageDiceGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Button CalculateDamageStatsButton;
        private Label AverageDamageEncounterLabel;
        private TextBox AverageDamageEncounterTextbox;
        private GroupBox groupBox1;
        private Label AverageAccuracyLabel;
        private Label AverageDamageAttackLabel;
        private TextBox AverageAccuracyTextbox;
        private Label AverageDamageRoundLabel;
        private TextBox AverageDamageAttackTextbox;
        private TextBox AverageDamageRoundTextbox;
        private GroupBox groupBox2;
        private Label label2;
        private Label label3;
        private TextBox textBox2;
        private Label label4;
        private TextBox textBox3;
        private TextBox textBox4;
        private GroupBox SettingsGroupBox;
        private GroupBox DamageDiceGroupBox;
        private Button DamageDiceEditButton;
        private Button DamageDiceDeleteButton;
        private Button DamageDiceAddButton;
        private TextBox DamageDiceTextBox;
        private ListBox DamageDiceListBox;
    }
}