param($installPath, $toolsPath, $package, $project)

$project.ProjectItems.Item("lapack_win32.dll").Properties.Item("CopyToOutputDirectory").Value = 2
$project.ProjectItems.Item("blas_win32.dll").Properties.Item("CopyToOutputDirectory").Value = 2