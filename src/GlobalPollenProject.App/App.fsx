#r "bin/Debug/netstandard1.6/GlobalPollenProject.Core.dll"
#r "bin/Debug/netstandard1.6/GlobalPollenProject.Persistence.dll"
#r "bin/Debug/netstandard1.6/GlobalPollenProject.App.dll"

open GlobalPollenProject.App.UseCases

let res = GlobalPollenProject.App.UseCases.Taxonomy.getByName "Compositae" None None