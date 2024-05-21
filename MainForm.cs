using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection.Emit;
using System.Windows.Forms;
using System.Diagnostics;

namespace Compiler
{
    public partial class MainForm : Form
    {
        Stream fileStream;
        public string filePath;
        bool isEdited = false;

        public MainForm()
        {
            InitializeComponent();

            this.KeyPreview = true;

            this.InputLanguageChanged += (sender, e) =>
            {
                languageKeyLabel.Text = string.Format("Язык ввода: {0}", InputLanguage.CurrentInputLanguage.LayoutName);
            };
            CapsLockLabel.Text = string.Format("Клавиша CapsLock: " + (Control.IsKeyLocked(Keys.CapsLock) ? "Нажата" : "Не нажата"));
            languageKeyLabel.Text = string.Format("Язык ввода: {0}", InputLanguage.CurrentInputLanguage.LayoutName);

            this.MainForm_Resize(null, null);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isEdited) {}
            else
            {
                SaveForm saveForm = new SaveForm(this);

                saveForm.ShowDialog();

                if (saveForm.DialogResult == DialogResult.OK)
                {
                    e.Cancel = false;
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            mainSplitContainer.Size = new Size(this.Size.Width - 40, this.Height - 130);
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            CapsLockLabel.Text = string.Format("Клавиша CapsLock: " + (Control.IsKeyLocked(Keys.CapsLock) ? "Нажата" : "Не нажата"));
        }

        private void NewButton_Click(object sender, EventArgs e)
        {
            if (richTextBox1.TextLength > 0)
            {
                richTextBox1.Text = "";
            }
            dataGridView2.Rows.Clear();
            errorStripStatusLabel1.Text = "";
        }

        private void AboutButton_Click(object sender, EventArgs e)
        {
            var InfoForm = new InfoForm();
            InfoForm.Show();
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            if (richTextBox1.TextLength > 0)
            {
                richTextBox1.SelectAll();
                richTextBox1.Copy();
            }
        }

        private void CutButton_Click(object sender, EventArgs e)
        {
            if (richTextBox1.TextLength > 0)
            {
                richTextBox1.SelectAll();
                richTextBox1.Cut();
            }
        }

        private void InsertButton_Click(object sender, EventArgs e)
        {
            richTextBox1.Paste();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            richTextBox1.Undo();
        }

        private void RepeatButton_Click(object sender, EventArgs e)
        {
            richTextBox1.Redo();
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            var fileContent = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "txt files (*.txt)|*.txt";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;
                    fileStream = openFileDialog.OpenFile();

                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        fileContent = reader.ReadToEnd();
                    }
                }
            }
            richTextBox1.Text = fileContent;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = "c:\\";
                saveFileDialog.Filter = "txt files (*.txt)|*.txt";
                saveFileDialog.FilterIndex = 2;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = saveFileDialog.FileName;
                    var fileStream = saveFileDialog.OpenFile();

                    using (StreamWriter writer = new StreamWriter(fileStream))
                    {
                        writer.Write(richTextBox1.Text);
                    }
                }
            }
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            string input = richTextBox1.Text;

            Dictionary<LexemeType, int> lexemeCodes = new Dictionary<LexemeType, int>()
            {
                { LexemeType.Keyword, 1 },
                { LexemeType.Keyword_val, 2 },
                { LexemeType.Identifier, 3 },
                { LexemeType.Delimiter, 4 },
                { LexemeType.Colon, 5 },
                { LexemeType.DataType, 6 },
                { LexemeType.Equally, 7 },
                { LexemeType.Minus, 8 },
                { LexemeType.Plus, 9 },
                { LexemeType.Number, 10 },
                { LexemeType.Semicolon, 11 },
                { LexemeType.Invalid, 12 },
                { LexemeType.EndStr, 13 },
                { LexemeType.NewStr, 14 }
            };

            string[] keywords = { "const" };
            string[] keywords_val = { "val" };
            string[] delimiters = { " " };
            string[] colons = { ":" };
            string[] dataTypes = { "Int" };
            string[] equallies = { "=" };
            string[] pluses = { "+" };
            string[] minuses = { "-" };
            string[] semicolones = { ";" };
            char[] endstrings = { '\r' };
            char[] newstrings = { '\n' };


            List<Lexeme> lexemes = new List<Lexeme>();

            int position = 0;
            while (position < input.Length)
            {
                bool found = false;

                //const
                foreach (string keyword in keywords)
                {
                    if (input.Substring(position).StartsWith(keyword))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Keyword], LexemeType.Keyword, input, position, position + keyword.Length - 1));
                        position += keyword.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //val
                foreach (string keyword_val in keywords_val)
                {
                    if (input.Substring(position).StartsWith(keyword_val))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Keyword_val], LexemeType.Keyword_val, input, position, position + keyword_val.Length - 1));
                        position += keyword_val.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //:
                foreach (string op in colons)
                {
                    if (input.Substring(position).StartsWith(op))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Colon], LexemeType.Colon, input, position, position + op.Length - 1));
                        position += op.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //data type
                foreach (string dataType in dataTypes)
                {
                    if (input.Substring(position).StartsWith(dataType))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.DataType], LexemeType.DataType, input, position, position + dataType.Length - 1));
                        position += dataType.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //=
                foreach (string op in equallies)
                {
                    if (input.Substring(position).StartsWith(op))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Equally], LexemeType.Equally, input, position, position + op.Length - 1));
                        position += op.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //+
                foreach (string op in pluses)
                {
                    if (input.Substring(position).StartsWith(op))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Equally], LexemeType.Plus, input, position, position + op.Length - 1));
                        position += op.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //-
                foreach (string op in minuses)
                {
                    if (input.Substring(position).StartsWith(op))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Equally], LexemeType.Minus, input, position, position + op.Length - 1));
                        position += op.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //;
                foreach (string op in semicolones)
                {
                    if (input.Substring(position).StartsWith(op))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Semicolon], LexemeType.Semicolon, input, position, position + op.Length - 1));
                        position += op.Length;
                        found = true;
                        break;
                    }
                }



                if (found) continue;

                //_
                foreach (string delimiter in delimiters)
                {
                    if (input.Substring(position).StartsWith(delimiter))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Delimiter], LexemeType.Delimiter, input, position, position + delimiter.Length - 1));
                        position += delimiter.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //  \0
                foreach (char endstr in endstrings)
                {
                    if (input[position] == endstr)
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.EndStr], LexemeType.EndStr, input, position, position));
                        position++;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //  \n
                foreach (char newstr in newstrings)
                {
                    if (input[position] == newstr)
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.NewStr], LexemeType.NewStr, input, position, position));
                        position++;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //name
                if (char.IsLetter(input[position]))
                {
                    int start = position;
                    while (position < input.Length && char.IsLetterOrDigit(input[position]))
                    {
                        position++;
                    }
                    string identifier = input.Substring(start, position - start);
                    lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Identifier], LexemeType.Identifier, input, start, position - 1));
                }
                //value
                else if (char.IsDigit(input[position]))
                {
                    int start = position;
                    while (position < input.Length && char.IsDigit(input[position]))
                    {
                        position++;
                    }
                    string number = input.Substring(start, position - start);
                    lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Number], LexemeType.Number, input, start, position - 1));
                }
                //error
                else
                {
                    string invalid = input[position].ToString();
                    lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Invalid], LexemeType.Invalid, input, position, position));
                    position++;
                }
            }

            dataGridView2.Rows.Clear();//*

            Parser parser = new Parser(lexemes);

            parser.Parse(dataGridView2);//*b

            errorStripStatusLabel1.Text = "Количество ошибок: " + parser.counter;
            if (parser.counter == 0)
            {
                dataGridView2.Rows.Add("Ошибок нет");//*
            }
        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            var HelpForm = new HelpForm();
            HelpForm.Show();
        }

        private void создатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (richTextBox1.TextLength > 0)
            {
                richTextBox1.Text = "";
            }
            dataGridView2.Rows.Clear();
            errorStripStatusLabel1.Text = "";
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fileContent = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "txt files (*.txt)|*.txt";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = openFileDialog.FileName;
                    fileStream = openFileDialog.OpenFile();

                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        fileContent = reader.ReadToEnd();
                    }
                }
            }
            richTextBox1.Text = fileContent;
        }

        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = "c:\\";
                saveFileDialog.Filter = "txt files (*.txt)|*.txt";
                saveFileDialog.FilterIndex = 2;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = saveFileDialog.FileName;
                    var fileStream = saveFileDialog.OpenFile();

                    using (StreamWriter writer = new StreamWriter(fileStream))
                    {
                        writer.Write(richTextBox1.Text);
                    }
                }
            }
        }

        private void сохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = "c:\\";
                saveFileDialog.Filter = "txt files (*.txt)|*.txt";
                saveFileDialog.FilterIndex = 2;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePath = saveFileDialog.FileName;
                    var fileStream = saveFileDialog.OpenFile();

                    using (StreamWriter writer = new StreamWriter(fileStream))
                    {
                        writer.Write(richTextBox1.Text);
                    }
                }
            }
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.FormClosing -= MainForm_FormClosing;

            if (isEdited)
            {
                Close();
            }
            else
            {
                SaveForm saveForm = new SaveForm(this);

                saveForm.ShowDialog();

                if (saveForm.DialogResult == DialogResult.OK)
                {
                    Close();
                }
            }
        }

        private void отменитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Undo();
        }

        private void повторитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Redo();
        }

        private void вырезатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (richTextBox1.TextLength > 0)
            {
                richTextBox1.SelectAll();
                richTextBox1.Cut();
            }
        }

        private void копироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (richTextBox1.TextLength > 0)
            {
                richTextBox1.SelectAll();
                richTextBox1.Copy();
            }
        }

        private void вставитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Paste();
        }

        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (richTextBox1.TextLength > 0)
            {
                richTextBox1.Text = "";
            }
        }

        private void выделитьВсеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (richTextBox1.TextLength > 0)
            {
                richTextBox1.SelectAll();
            }
        }

        private void постановкаЗадачиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string htmlFilePath = @"C:\Users\Vadim\Desktop\Compiler-CourseWork\Resources\htmls\task.html";
            
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = htmlFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при попытке открыть html документ: {ex.Message}");
            }
        }
        private void грамматикаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string htmlFilePath = @"C:\Users\Vadim\Desktop\Compiler-CourseWork\Resources\htmls\Grammar.html";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = htmlFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при попытке открыть html документ: {ex.Message}");
            }
        }
        private void классификацияГрамматикиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string htmlFilePath = @"C:\Users\Vadim\Desktop\Compiler-CourseWork\Resources\htmls\GrammarClassification.html";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = htmlFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при попытке открыть html документ: {ex.Message}");
            }
        }
        private void методАнализаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string htmlFilePath = @"C:\Users\Vadim\Desktop\Compiler-CourseWork\Resources\htmls\MethodOfAnalisys.html";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = htmlFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при попытке открыть html документ: {ex.Message}");
            }
        }
        private void диагностикаИНейтрализацияОшибокToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string htmlFilePath = @"C:\Users\Vadim\Desktop\Compiler-CourseWork\Resources\htmls\DiagnosticsAndNeutralization.html";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = htmlFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при попытке открыть html документ: {ex.Message}");
            }
        }
        private void тестовыйПримерToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string htmlFilePath = @"C:\Users\Vadim\Desktop\Compiler-CourseWork\Resources\htmls\Examples.html";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = htmlFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при попытке открыть html документ: {ex.Message}");
            }
        }
        private void списокЛитературыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string htmlFilePath = @"C:\Users\Vadim\Desktop\Compiler-CourseWork\Resources\htmls\Literature.html";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = htmlFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при попытке открыть html документ: {ex.Message}");
            }
        }
        private void исходныйКодПрограммыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start($"https://github.com/Skywhisp/CourseWork_Compiler");
        }
        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var InfoForm = new InfoForm();
            InfoForm.Show();
        }
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var HelpForm = new HelpForm();
            HelpForm.Show();
        }
        private void пускToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input = richTextBox1.Text;

            Dictionary<LexemeType, int> lexemeCodes = new Dictionary<LexemeType, int>()
            {
                { LexemeType.Keyword, 1 },
                { LexemeType.Keyword_val, 2 },
                { LexemeType.Identifier, 3 },
                { LexemeType.Delimiter, 4 },
                { LexemeType.Colon, 5 },
                { LexemeType.DataType, 6 },
                { LexemeType.Equally, 7 },
                { LexemeType.Minus, 8 },
                { LexemeType.Plus, 9 },
                { LexemeType.Number, 10 },
                { LexemeType.Semicolon, 11 },
                { LexemeType.Invalid, 12 },
                { LexemeType.EndStr, 13 },
                { LexemeType.NewStr, 14 }
            };

            string[] keywords = { "const" };
            string[] keywords_val = { "val" };
            string[] delimiters = { " " };
            string[] colons = { ":" };
            string[] dataTypes = { "Int" };
            string[] equallies = { "=" };
            string[] pluses = { "+" };
            string[] minuses = { "-" };
            string[] semicolones = { ";" };
            char[] endstrings = { '\r' };
            char[] newstrings = { '\n' };

            List<Lexeme> lexemes = new List<Lexeme>();

            int position = 0;
            while (position < input.Length)
            {
                bool found = false;

                //const
                foreach (string keyword in keywords)
                {
                    if (input.Substring(position).StartsWith(keyword))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Keyword], LexemeType.Keyword, input, position, position + keyword.Length - 1));
                        position += keyword.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //val
                foreach (string keyword_val in keywords_val)
                {
                    if (input.Substring(position).StartsWith(keyword_val))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Keyword_val], LexemeType.Keyword_val, input, position, position + keyword_val.Length - 1));
                        position += keyword_val.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //:
                foreach (string op in colons)
                {
                    if (input.Substring(position).StartsWith(op))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Colon], LexemeType.Colon, input, position, position + op.Length - 1));
                        position += op.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //data type
                foreach (string dataType in dataTypes)
                {
                    if (input.Substring(position).StartsWith(dataType))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.DataType], LexemeType.DataType, input, position, position + dataType.Length - 1));
                        position += dataType.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //=
                foreach (string op in equallies)
                {
                    if (input.Substring(position).StartsWith(op))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Equally], LexemeType.Equally, input, position, position + op.Length - 1));
                        position += op.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //+
                foreach (string op in pluses)
                {
                    if (input.Substring(position).StartsWith(op))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Equally], LexemeType.Plus, input, position, position + op.Length - 1));
                        position += op.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //-
                foreach (string op in minuses)
                {
                    if (input.Substring(position).StartsWith(op))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Equally], LexemeType.Minus, input, position, position + op.Length - 1));
                        position += op.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //;
                foreach (string op in semicolones)
                {
                    if (input.Substring(position).StartsWith(op))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Semicolon], LexemeType.Semicolon, input, position, position + op.Length - 1));
                        position += op.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //_
                foreach (string delimiter in delimiters)
                {
                    if (input.Substring(position).StartsWith(delimiter))
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Delimiter], LexemeType.Delimiter, input, position, position + delimiter.Length - 1));
                        position += delimiter.Length;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //  \0
                foreach (char endstr in endstrings)
                {
                    if (input[position] == endstr)
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.EndStr], LexemeType.EndStr, input, position, position));
                        position++;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //  \n
                foreach (char newstr in newstrings)
                {
                    if (input[position] == newstr)
                    {
                        lexemes.Add(new Lexeme(lexemeCodes[LexemeType.NewStr], LexemeType.NewStr, input, position, position));
                        position++;
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                //name
                if (char.IsLetter(input[position]))
                {
                    int start = position;
                    while (position < input.Length && char.IsLetterOrDigit(input[position]))
                    {
                        position++;
                    }
                    string identifier = input.Substring(start, position - start);
                    lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Identifier], LexemeType.Identifier, input, start, position - 1));
                }
                //value
                else if (char.IsDigit(input[position]))
                {
                    int start = position;
                    while (position < input.Length && char.IsDigit(input[position]))
                    {
                        position++;
                    }
                    string number = input.Substring(start, position - start);
                    lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Number], LexemeType.Number, input, start, position - 1));
                }
                //error
                else
                {
                    string invalid = input[position].ToString();
                    lexemes.Add(new Lexeme(lexemeCodes[LexemeType.Invalid], LexemeType.Invalid, input, position, position));
                    position++;
                }
            }

            dataGridView2.Rows.Clear();//*

            Parser parser = new Parser(lexemes);

            parser.Parse(dataGridView2);//*b

            errorStripStatusLabel1.Text = "Количество ошибок: " + parser.counter;
            if (parser.counter == 0)
            {
                dataGridView2.Rows.Add("Ошибок нет");//*
            }
        }

        private void resizeFunction()
        {
            richTextBox1.Size = new Size(mainSplitContainer.Width - 10, mainSplitContainer.Size.Height);
            dataGridView2.Size = new Size(mainSplitContainer.Width - 10, mainSplitContainer.Size.Height - mainSplitContainer.Panel1.Size.Height - 10 - statusStrip1.Height);
        }

        private void mainSplitContainer_Resize(object sender, EventArgs e)
        {
            resizeFunction();
        }

        private void mainSplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
        {
            resizeFunction();
        }
    }
}