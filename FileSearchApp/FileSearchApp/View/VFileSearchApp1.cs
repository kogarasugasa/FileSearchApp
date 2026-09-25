using System;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CustomFunctions;
using CSharpNized.Rust.std;

namespace FileSearchApp
{
    public partial class VFileSearchApp : Form
    {
        public void InitializeEvent()
        {
            // ============================================================
            // = 説明：Viewからのイベント
            // ============================================================
            this.Load += (s, e) =>
            {
                var records = _vm.GetTabRecords();
                if (records.Count == 0)
                {
                    _vm.OnAddClick();
                }
                else
                {
                    foreach (var record in records)
                    {
                        var lbl = this.AddTab();
                        //lbl.Text = MsPGothic.GetAdjustedText(record.Text, lbl.Width);
                        this.AllocateTab(lbl, record);
                    }
                    _vm.OnSelectClick(records.First().Key);
                }
            };
            this.Shown += (s, e) =>
            {
                this.LoadTheme();
                _searchWordsTxtbox.Focus();
            };
            this.FormClosing += (s, e) =>
            {
                switch (e.CloseReason)
                {
                    case CloseReason.UserClosing: // ユーザーが操作した
                        if (_vm.Record.FileSearch.IsReadOnly)
                        {
                            this.LoadTheme();
                            var msg = "現在読取専用のためデータは保存されません";
                            var button = MessageBoxButtons.OKCancel;
                            var msgResult = MessageBox.Show(msg, "読取専用", button);
                            if (msgResult != DialogResult.OK)
                            {
                                e.Cancel = true;
                                return;
                            }
                        }
                        break;
                    case CloseReason.None: // 謎
                    case CloseReason.WindowsShutDown: // サインアウトされそう
                    case CloseReason.MdiFormClosing: // MDI親フォームが閉じそう this.Parent
                    case CloseReason.TaskManagerClosing: // タスクマネージャーから消されそう
                    case CloseReason.FormOwnerClosing: // 所有者が閉じられそう this.Owner
                    case CloseReason.ApplicationExitCall: // Called Application.Exit()
                    default:
                        break;
                }
                this.Enabled = false;
                _vm.Record.FileSearch.CancelSearch();
                _parentDispose();
                this.Dispose();
            };
            // 以下、別のウィンドウから戻ってきたときにがくがくになる問題の対策
            bool _isSearchingDeactive = false;
            int _listSizeDeactive = 0;
            this.Activated += (s, e) =>
            {
                try
                {
                    if (!_isSearchingDeactive)
                    {
                        return;
                    }
                    if (_listSizeDeactive == _listView.VirtualListSize)
                    {
                        return;
                    }
                    ;
                    if (_vm.Record.ListView.SelectedIndex == -1)
                    {
                        return;
                    }
                    _listView.VirtualListSize = 0;
                    _listView.VirtualListSize = _vm.Record.ListView.Count;
                    var dispIndices = _listView.GetDisplayIndices();
                    if (dispIndices.Count != 0)
                    {
                        var rowCount = _listView.GetDispRowsCount();
                        var dispLastRow = dispIndices.Min() + rowCount - 2;
                        // DispLayIndices の方が VirtualListSize より後に取得しているため
                        // VirtualListSize を超える値が存在する可能性がある
                        dispLastRow = Math.Min(dispLastRow, _listView.VirtualListSize - 1);
                        _listView.EnsureVisible(dispLastRow);
                    }
                }
                catch (Exception ex)
                {
                    MMessageBox.Show(ex.Message);
                }
                finally
                {
                    _isSearchingDeactive = false;
                    _listSizeDeactive = 0;
                }
            };
            this.Deactivate += (s, e) =>
            {
                _isSearchingDeactive = !_vm.Record.FileSearch.IsStandby;
                _listSizeDeactive = _listView.VirtualListSize;
            };
            // 以上、別のウィンドウから戻ってきたときにがくがくになる問題の対策
            this.SizeChanged += (s, e) =>
            {
                _searchWordsTxtbox.Width = this.Width - _searchWordsTxtbox.Left - 26;
                _listView.Height = this.Height - _listView.Top - 50;
                _listView.Width = this.Width - 36;
                if (this.WindowState == FormWindowState.Normal)
                {
                    _vmWindow.WindowProperty = new WindowRectangle(this);
                }
                else
                {
                    _vmWindow.WindowProperty.WindowState = this.WindowState;
                }
            };
            this.Move += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Normal)
                {
                    _vmWindow.WindowProperty = new WindowRectangle(this);
                }
                else
                {
                    _vmWindow.WindowProperty.WindowState = this.WindowState;
                }
            };
            this.ContextMenuStrip.Opening += (s, e) =>
            {
                // ボタン以外では開かないようにする
                if (_optionBtn.Focused == false && _filterBtn.Focused == false)
                {
                    e.Cancel = true;
                }
            };
            this.MouseClick += async (s, e) =>
            {
                // デバッグ用出力確認用
                if (e.Button == MouseButtons.Middle)
                {
                    await Task.Run(() => _vm.Record.FileSearch.ShowDebug());
                }
            };
            this.DoubleClick += (s, e) =>
            {
                _vm.OnAddClick();
            };
            _optionBtn.Click += (s, e) =>
            {
                _vm.Record.Option.OnOptionMenuClick();
                this.ContextMenuStrip.Show(
                    this,
                    _optionBtn.Left,
                    _optionBtn.Top + _optionBtn.Height - 2
                );
                //右クリックでメニューを出さないためにボタンのフォーカスで判断するので
                //別のコントロールに移す
                _searchWordsTxtbox.Focus();
            };
            _filterBtn.Click += (s, e) =>
            {
                this.ContextMenuStrip.Items.Clear();
                _vm.Record.FileSearch.ReloadFilterMenu();
                this.ContextMenuStrip.Items.AddRange(_vm.Record.FileSearch.FilterMenuItems);
                this.ContextMenuStrip.Show(
                    this,
                    _filterBtn.Left,
                    _filterBtn.Top + _filterBtn.Height - 2
                );
                //右クリックでメニューを出さないためにボタンのフォーカスで判断するので
                //別のコントロールに移す
                _searchWordsTxtbox.Focus();
            };
            _aliasCheckBox.CheckedChanged += (s, e) =>
            {
                _vm.Record.ListView.AliasEnabled = ((CheckBox)s).Checked;
                _vm.Record.FileSearch.UpdateViewRangeAlias(
                    _listView.GetDisplayIndices(),
                    _vm.Record.ListView
                );
                _listView.RedrawItems();
            };
            _getClipBtn.Click += (s, e) =>
            {
                _searchWordsTxtbox.Text = _vm.Record.FileSearch.GetClip();
                _searchWordsTxtbox.Focus();
            };
            _runSearchBtn.MouseClick += async (s, e) =>
            {
                await this.StartSearchAsync(e.Button);
            };
            _clearBtn.Click += (s, e) =>
            {
                if (_searchWordsTxtbox.Text == "")
                {
                    _searchWordsTxtbox.ShowCandidate(_vmWindow.SearchCandidate);
                    _searchWordsTxtbox.Focus();
                    return;
                }
                _searchWordsTxtbox.Text = string.Empty;
                _searchWordsTxtbox.Focus();
            };
            _clearBtn.MouseMove += (s, e) =>
            {
                //_clearBtn.BackColor = SystemColors.ControlLight; //ボタンと同じ色
                var color = Color.FromArgb(100, Color.LightSkyBlue);
                if (_clearBtn.BackColor == color)
                {
                    return;
                }
                _clearBtn.BackColor = color;
            };
            _clearBtn.MouseLeave += (s, e) => _clearBtn.BackColor = Color.White;
            _clearBtn.MouseDown += (s, e) =>
            {
                _clearBtn.BackColor = Color.PowderBlue;
            };
            _searchWordsTxtbox.GotFocus += (s, e) =>
            {
                Task.Run(() =>
                {
                    Task.Delay(10).Wait();
                    _searchWordsTxtbox.SelectAll();
                });
            };
            _searchWordsTxtbox.TextChanged += (s, e) =>
            {
                var text = _searchWordsTxtbox.Text;
                if (_vm.Record.FileSearch.ValidatingSearchWords(text))
                {
                    var defTxtBox = new TextBox();
                    _searchWordsTxtbox.BackColor = defTxtBox.BackColor;
                    defTxtBox.Dispose();
                }
                else
                {
                    _searchWordsTxtbox.BackColor = System.Drawing.Color.LightPink;
                }
                _vm.Record.FileSearch.SearchWords = text;
            };
            _searchWordsTxtbox.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    e.Handled = true;
                }
            };
            _searchWordsTxtbox.KeyDown += async (s, e) =>
            {
                if (e.Handled)
                {
                    return;
                }
                switch (e.KeyCode)
                {
                    case Keys.Enter:
                        if (CandidateTextListBox.Contains(_searchWordsTxtbox))
                        {
                            return;
                        }
                        if (_searchWordsTxtbox.IsOptionKeyDown())
                        {
                            await StartSearchAsync(MouseButtons.Right);
                        }
                        else
                        {
                            await StartSearchAsync(MouseButtons.Left);
                        }
                        _searchWordsTxtbox.SelectAll();
                        break;
                    case Keys.Down:
                        if (CandidateTextListBox.Contains(_searchWordsTxtbox))
                        {
                            return;
                        }
                        else
                        {
                            _searchWordsTxtbox.ShowCandidate(_vmWindow.SearchCandidate);
                        }
                        break;
                }
            };
            _listView.GotFocus += (s, e) =>
            {
                if (_vm.Record.ListView.SelectedIndex < 0)
                {
                    _vm.Record.ListView.SelectItem(0);
                }
            };
            _listView.ItemSelectionChanged += (s, e) =>
            {
                if (!e.IsSelected)
                {
                    return;
                }
                int beforeSelection = _vm.Record.ListView.SelectedIndex;
                _vm.Record.ListView.SelectItem(e.ItemIndex);
                e.Item.Selected = false;
                e.Item.Focused = false;
                _listView.SelectedIndices.Clear();
                //_vm.Record.ListView.RedrawItems(_listView); //表示範囲すべてだと全画面のときラグい
                _vm.Record.ListView.RedrawItem(_listView, e.ItemIndex);
                _vm.Record.ListView.RedrawItem(_listView, beforeSelection);
            };
            _listView.KeyDown += (s, e) =>
            {
                e.Handled = true;
                if (e.KeyCode == Keys.Down)
                {
                    var selection = _vm.Record.ListView.SelectedIndex;
                    var last = _vm.Record.ListView.Count - 1;
                    if (selection >= last)
                    {
                        return;
                    }
                    int nextIdx = selection + 1;
                    _listView.SelectedIndices.Add(nextIdx);
                    _listView.EnsureVisible(nextIdx);
                }
                else if (e.KeyCode == Keys.Up)
                {
                    if (_vm.Record.ListView.SelectedIndex <= 0)
                    {
                        return;
                    }
                    int nextIdx = _vm.Record.ListView.SelectedIndex - 1;
                    _listView.SelectedIndices.Add(nextIdx);
                    _listView.EnsureVisible(nextIdx);
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    _vm.Record.ListView.OpenSelectionItem();
                }
            };
            _listView.RetrieveVirtualItem += (s, e) =>
            {
                // 先に alias を更新
                _vm.Record.FileSearch.UpdateViewRangeAlias(e.ItemIndex, _vm.Record.ListView);
                var itemNew = _vm.Record.ListView.GetListViewItem(e.ItemIndex);
                // リストビューにタブでフォーカスした場合に先頭行にレティクルが表示される問題の対策用
                if (e.ItemIndex == 0 && _topItem.IsNone())
                {
                    _topItem = Some.New(itemNew);
                }
                _topItem.IfSome(some => some.Focused = false);
                // 背景色の設定
                if (_vm.Record.ListView.IsSelected(e.ItemIndex))
                {
                    itemNew.BackColor = Color.SkyBlue;
                    itemNew.Selected = false;
                    itemNew.Focused = false;
                }
                else if (_vm.Record.ListView.GetExFileInfo(e.ItemIndex).Invalided)
                {
                    itemNew.BackColor = Color.MistyRose;
                }
                else
                {
                    itemNew.BackColor = Color.White;
                }
                // 文字色の設定
                if (_vm.Record.ListView.DataSource.Filters.Count != 0)
                {
                    itemNew.ForeColor = Color.Blue;
                }
                e.Item = itemNew;
            };
            _listView.ColumnClick += (s, e) =>
            {
                _vm.Record.ListView.SortListView(_listView, e.Column);
                _listView.VirtualListSize = 0;
                _listView.VirtualListSize = _vm.Record.ListView.Count;
            };
            _listView.DoubleClick += (s, e) =>
            {
                _vm.Record.ListView.OpenSelectionItem();
            };
            _listView.MouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Right)
                {
                    return;
                }
                _vm.Record.ListView.ReloadListViewMenu();
                _listView.ContextMenuStrip.Items.Clear();
                _listView.ContextMenuStrip.Items.AddRange(_vm.Record.ListView.ListViewMenuItems);
            };
            _listView.MouseClick += (s, e) =>
            {
                if (_vm.Record.FileSearch.IsReadOnly && this.BackColor == _defFormColor)
                {
                    var msg = "ロックの取得に失敗しました。読取専用に変更します" +
                        Environment.NewLine +
                        "再度ロックを取得したい場合はメニューから" +
                        "「読取専用⇔保存可能」を選択してください"
                    ;
                    MMessageBox.Show(msg);
                    this.LoadTheme();
                }
            };
            _listView.ContextMenuStrip.Opening += (s, e) =>
            {
                if (_listView.IsMouseOverColumnHeader())
                {
                    e.Cancel = true;
                    Task task;
                    _listView.GetPointingHeaderText().Match(
                        none => CustomLogger.WriteLine("Columnheader.Text の取得に失敗しました"),
                        some => task = _vm.Record.ListView.ShowFilterCommandLine(some)
                    );
                    return;
                }
                if (_vm.Record.ListView.SelectedIndex >= _vm.Record.ListView.DataSource.Count)
                {
                    e.Cancel = true;
                    return;
                }
                if (_vm.Record.ListView.SelectedIndex < 0)
                {
                    e.Cancel = true;
                    return;
                }
            };
            _listView.DragDrop += (s, e) =>
            {
                var paths = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                if (paths.Any())
                {
                    MMessageTransceiver.SendMessage(paths.First());
                }
            };
            _listView.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            };
        }
        /// <summary>
        /// 検索開始処理
        /// </summary>
        async Task StartSearchAsync(MouseButtons pButtons)
        {
            if (!_vm.Record.FileSearch.IsStandby)
            {
                _vm.Record.FileSearch.CancelSearch();
                return;
            }
            if (_vm.Record.ListView.DataSource.Filters.Count != 0)
            {
                await MMessageBox.ShowAsync("フィルタが適用されたまま検索を実行します");
            }
            _vm.Record.ListView.Clear();
            _searchWordsTxtbox.Focus();
            bool isButtonRight = pButtons == MouseButtons.Right;
            bool isSearchTextEmpty = _vm.Record.FileSearch.SearchWords.Length == 0;
            if (isButtonRight && !isSearchTextEmpty)
            {
                await _vm.Record.FileSearch.StartFileSearchFolderSelectionAsync();
            }
            else if (isButtonRight && isSearchTextEmpty)
            {
                await _vm.Record.FileSearch.StartPowerShellSearchAsync();
            }
            else if (!isButtonRight && !isSearchTextEmpty)
            {
                await _vm.Record.FileSearch.StartFileSearchAsync();
            }
            else if (!isButtonRight && isSearchTextEmpty)
            {
                await _vm.Record.FileSearch.StartTagSearchAsync();
            }
            // タブの表示を変更する
            _vm.ChangeTagName();
            for (int i = 0; i < this.Controls.Count; i++)
            {
                var ctrl = this.Controls[i];
                if (ctrl.Tag == _controlTag_Tab && ctrl.Name == _vm.Record.Key)
                {
                    ctrl.Text = this.GetTabText(_vm.Record.Text, ctrl.Width);
                }
            }
            _vm.Record.ListView.RiseDataSourceChange(true);
        }
        public void LoadTheme()
        {
            if (_vm.Record.FileSearch.IsReadOnly)
            {
                this.InvokeIfRequiredElseNonInvoke(() =>
                {
                    this.BackColor = _readOnlyFormColor;
                    _filterBtn.UseVisualStyleBackColor = true;
                    _optionBtn.UseVisualStyleBackColor = true;
                    _getClipBtn.UseVisualStyleBackColor = true;
                    _runSearchBtn.UseVisualStyleBackColor = true;
                });
            }
            else
            {
                this.InvokeIfRequiredElseNonInvoke(() =>
                {
                    using (var defForm = new Form())
                    {
                        this.BackColor = _defFormColor;
                    }
                    _filterBtn.UseVisualStyleBackColor = true;
                    _optionBtn.UseVisualStyleBackColor = true;
                    _getClipBtn.UseVisualStyleBackColor = true;
                    _runSearchBtn.UseVisualStyleBackColor = true;
                });
            }
        }
    }
}