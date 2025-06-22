module OpenAIClient

open Fable.Core
open Fable.Core.JsInterop
open System
open Types

[<Import("*", from="./openaiRealtime.js")>]
let realtimeClient: obj = jsNative

type ConversationUpdate = 
    | SessionStarted
    | InputSpeechStarted
    | InputSpeechFinished
    | OutputDelta of audioBytes: byte[] option * text: string option
    | ResponseFinished
    | ToolCall of functionName: string * args: string * callId: string

type RealtimeManager = 
    abstract startSession: string -> (CarDescriptor -> unit) -> (string -> unit) -> JS.Promise<unit>
    abstract sendAudio: byte[] -> JS.Promise<unit>
    abstract updateModel: CarDescriptor -> JS.Promise<unit>
    abstract dispose: unit -> unit

let manager: RealtimeManager = unbox realtimeClient
