using IniParser;
using IniParser.Model;
using System.Diagnostics;
using OpenMcdf;
using NLog;

string strExeFilePath = Application.ExecutablePath;
string strExeFileName = Path.GetFileName(strExeFilePath);
string strWorkPath = Path.GetDirectoryName(strExeFilePath);
string strIniConfigFilename = "VPinballX.starter.ini";

var config = new NLog.Config.LoggingConfiguration();
var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "VPinballX.starter.log" };
config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logfile);
LogManager.Configuration = config;
var logger = LogManager.GetCurrentClassLogger();

try
{

    var parser = new FileIniDataParser();

    string strSettingsIniFilePath = System.IO.Path.Combine(strWorkPath, $"{strIniConfigFilename}");

    if (!FileOrDirectoryExists(strSettingsIniFilePath))
    {
        string strDefaultIniConfig =
@";A Configuration file for VPinballX.starter
[VPinballX.starter]
;DefaultVersion when started without any table param.
DefaultVersion=10.80
[VPinballX]
;Default value used when not found in the table below.
Default=VPinballX74.exe
;File versions converted to the right VPinballXxx.exe
10.60=VPinballX74.exe
10.70=VPinballX74.exe
10.72=VPinballX74.exe
10.80=VPinballX85.exe";

        string strWelcomeString =
$@"Welcome new VPinballX.starter user!

We could not find a {strIniConfigFilename} next to the {strExeFileName} file. This file should look like this:

X-----X-----X-----X----X-----X-----X-----X-----X
{strDefaultIniConfig}
X-----X-----X-----X----X-----X-----X-----X-----X

With this information VPinballX.starter can be used as a replacement for VPinballX.exe. It works like this:

VPinballX.starter is started with exactly the same parameters as VPinballX.exe. First it loads the table file and finds out what version it was saved with. Then it takes that information
and looks in the [VPinballX] table above to find out which version of VPinballX.xxx.exe you want to start with. It will then run the VPinballX.xxx.exe that you have configured using exactly the same parameters.

If it cannot find a version in the table the default entry under [VPinballx] will be used. If you simply double-click the VPinballX.starter without any table, the Default under [VPinballx.starter] is used.

This way the correct table version or the version you have chosen will always be used. Each time you start VPinballX, a log entry will be added to VPinballX.starter.log, telling which version was used.

Do you want to create this file now?";

        DialogResult dialogResult = MessageBox.Show(strWelcomeString, $"{strExeFileName}: Welcome", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (dialogResult == DialogResult.Yes)
        {
            using (StreamWriter sw = File.CreateText(strSettingsIniFilePath))
            {
                sw.Write(strDefaultIniConfig);
            }
            MessageBox.Show($"The config file {strSettingsIniFilePath} is created. Please modify it too your needs.\n\nExiting.", $"{strExeFileName}: Welcome", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Environment.Exit(0);
        }
        if (!FileOrDirectoryExists(strSettingsIniFilePath) ){
            throw new FileNotFoundException($"Configuration \"{strSettingsIniFilePath}\"cannot be found!\n\nExiting");
        }
    }


    IniData versionTable = parser.ReadFile(strSettingsIniFilePath);

    string tableFilename = null;

    foreach (string arg in args)
    {
        if (arg.EndsWith(".vpx", StringComparison.OrdinalIgnoreCase))
        {
            tableFilename = arg;
            break;
        }
    }
    string defaultfileVersion = versionTable["VPinballX.starter"]["DefaultVersion"];

    if (object.Equals(defaultfileVersion, null))
    {
        throw new ArgumentException($"No\n\n[VPinballX.starter]\nDefaultVersion=10.xx\n\nfound in the ini! ({strSettingsIniFilePath})");
    }
    var fileVersion = Int32.Parse(defaultfileVersion.Replace(".", String.Empty));


    if (!object.Equals(tableFilename, null))
    {
        // Read the version of VPinballX which saved this table
        var cf = new CompoundFile(tableFilename);
        try
        {
            var gameStorage = cf.RootStorage.GetStorage("GameStg");
            var gameData = gameStorage.GetStream("GameData");

            fileVersion = BitConverter.ToInt32(gameStorage.GetStream("Version").GetData(), 0);
        }
        finally
        {
            cf.Close();
        }
    }
    string strFileVersion = $"{fileVersion / 100}.{fileVersion % 100}";
    string vpxcommand = versionTable["VPinballX"][strFileVersion];
    if (object.Equals(vpxcommand, null))
    {
        vpxcommand = versionTable["VPinballX"]["Default"];
        if (object.Equals(vpxcommand, null))
            throw new ArgumentException($"No\n\n[VPinballX]\n{strFileVersion}=VPinballXxx.exe\nor\n\n\n[VPinballX]\nDefault=VPinballXxx.exe\n\nfound in the ini! ({strSettingsIniFilePath})");
    }
    if (!Path.IsPathFullyQualified(vpxcommand))
    {
        vpxcommand = System.IO.Path.Combine(strWorkPath, vpxcommand);
    }

    if (!object.Equals(tableFilename, null))
        logger.Info($"Found table version {strFileVersion} of \"{tableFilename}\" mapped to \"{vpxcommand}\"");
    else
        logger.Info($"Using default version {strFileVersion} mapped to \"{vpxcommand}\"");

    StartAnotherProgram(vpxcommand, args);

}
catch (ArgumentException e)
{
    MessageBox.Show(e.Message, $"{strExeFileName}: Configuration error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
catch (FileNotFoundException e)
{
    MessageBox.Show(e.Message, $"{strExeFileName}: File not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
catch (Exception e)
{
    logger.Error(e, "Unknown error");
    MessageBox.Show(e.Message, $"{strExeFileName}: Unknown error", MessageBoxButtons.OK, MessageBoxIcon.Error);
}

bool FileOrDirectoryExists(string name)
{
    return Directory.Exists(name) || File.Exists(name);
}
void StartAnotherProgram(string programPath, string[] programArgs)
{
    //string Arguments = string.Join(" ", programArgs);

    Process process = new Process();

    ProcessStartInfo startInfo = new ProcessStartInfo
    {
        FileName = programPath,
        RedirectStandardOutput = false,
        RedirectStandardError = false,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    foreach (var arg in programArgs)
    {
        startInfo.ArgumentList.Add(arg);
    }


    process.StartInfo = startInfo;
    process.Start();
}
