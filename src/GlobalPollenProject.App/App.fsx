#r "bin/Debug/netstandard1.6/GlobalPollenProject.App.dll"

open GlobalPollenProject.App
open System

let base64ImageUpload = "e8923yiusanciuh 780ewhc7890qwn890cqrn890q"
let newGrainId = Guid.NewGuid ()

GrainAppService.submitUnknownGrain newGrainId base64ImageUpload
GrainAppService.identifyUnknownGrain newGrainId 2