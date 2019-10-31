using System;
namespace Umbrella2.Visualizer.Winforms
{
	public partial class TrackletOutput
	{
		/// <summary>
		/// Provides custom navigation according to the key pressed.
		/// </summary>
		/// <param name="Key">Pressed key char.</param>
		private void HandleKeyPress(char Key)
		{
			int Index;
			Key = char.ToUpper(Key);
			switch (Key)
			{
				/* Next tracklet */
				case 'S':
					if (checkedListBox1.SelectedIndex + 1 < checkedListBox1.Items.Count) checkedListBox1.SelectedIndex++;
					break;
				/* Previous tracklet */
				case 'W':
					if (checkedListBox1.SelectedIndex > 0) checkedListBox1.SelectedIndex--;
					break;
				/* Next object */
				case 'D':
					if (dataGridView1.SelectedRows.Count >= 1)
					{
						Index = dataGridView1.SelectedRows[0].Index; dataGridView1.ClearSelection();
						if (Index + 1 < dataGridView1.Rows.Count) dataGridView1.Rows[Index + 1].Selected = true;
						else dataGridView1.Rows[Index].Selected = true;
					}
					if (dataGridView1.SelectedRows.Count == 0) dataGridView1.Rows[0].Selected = true;
					break;
				/* Previous object */
				case 'A':
					if (dataGridView1.SelectedRows.Count >= 1)
					{
						Index = dataGridView1.SelectedRows[0].Index; dataGridView1.ClearSelection();
						if (Index > 0) dataGridView1.Rows[Index - 1].Selected = true;
						else dataGridView1.Rows[Index].Selected = true;
					}
					if (dataGridView1.SelectedRows.Count == 0) dataGridView1.Rows[dataGridView1.Rows.Count - 1].Selected = true;
					break;
				/* Previous image */
				case 'Q':
					var Keys = System.Linq.Enumerable.ToList(Images.Keys);
					Index = Keys.IndexOf(CurrentImageName);
					if (Index > 0) CurrentImageName = Keys[Index - 1];
					UpdateImage();
					break;
				/* Next image */
				case 'E':
					Keys = System.Linq.Enumerable.ToList(Images.Keys);
					Index = Keys.IndexOf(CurrentImageName);
					if (Index < Keys.Count - 1) CurrentImageName = Keys[Index + 1];
					UpdateImage();
					break;
				/* Select for reporting */
				case ' ':
					checkedListBox1.SetItemChecked(checkedListBox1.SelectedIndex, !checkedListBox1.GetItemChecked(checkedListBox1.SelectedIndex));
					break;
				/* Toggle blink */
				case 'B':
					BlinkOnDetection();
					break;
			}
		}

	}
}
