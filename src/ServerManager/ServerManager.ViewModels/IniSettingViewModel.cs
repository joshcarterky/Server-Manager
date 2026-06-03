using CommunityToolkit.Mvvm.ComponentModel;

namespace ServerManager.ViewModels;

public class IniSettingViewModel : ObservableObject
{
	private string _value = string.Empty;

	public string Category { get; }

	public string DisplayName { get; }

	public string VariableName { get; }

	public string Description { get; }

	public string FileName { get; }

	public string Section { get; }

	public string DataType { get; }

	public string DefaultValue { get; }

	public string DisplayDefaultValue
	{
		get
		{
			if (!string.IsNullOrEmpty(DefaultValue))
			{
				return DefaultValue;
			}
			return "(blank)";
		}
	}

	public string ValidRange { get; }

	public bool RequiresRestart { get; }

	public string Value
	{
		get
		{
			return _value;
		}
		set
		{
			SetProperty(ref _value, value, "Value");
		}
	}

	public IniSettingViewModel(string category, string displayName, string variableName, string description, string fileName, string section, string dataType, string defaultValue, string validRange, bool requiresRestart)
	{
		Category = category;
		DisplayName = displayName;
		VariableName = variableName;
		Description = description;
		FileName = fileName;
		Section = section;
		DataType = dataType;
		DefaultValue = defaultValue;
		ValidRange = validRange;
		RequiresRestart = requiresRestart;
		Value = defaultValue;
	}
}
