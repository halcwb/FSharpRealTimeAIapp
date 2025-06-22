module AudioUtils

open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open Fable.Core.JS
open System

[<Import("*", from="./audioProcessor.js")>]
let audioProcessor: obj = jsNative

type AudioProcessor = 
    abstract start: (byte[] -> unit) -> JS.Promise<obj>
    abstract setMute: obj -> bool -> unit
    abstract dispose: obj -> unit

let processor: AudioProcessor = unbox audioProcessor
