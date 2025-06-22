module Types

open System

type TyreStatus = 
    | NeedsReplacement
    | Worn  
    | Good
    | New

type TyreStatuses = {
    FrontLeft: TyreStatus option
    FrontRight: TyreStatus option
    BackLeft: TyreStatus option
    BackRight: TyreStatus option
}

type CarDescriptor = {
    Make: string option
    Model: string option
    Year: int option
    Mileage: int option
    ConditionNotes: string list
    Tyres: TyreStatuses
}

type MicStatus = 
    | Disconnected
    | Active
    | Muted

type Msg =
    | UpdateMake of string
    | UpdateModel of string
    | UpdateYear of int option
    | UpdateMileage of int option
    | AddConditionNote
    | UpdateConditionNote of int * string
    | RemoveConditionNote of int
    | UpdateTyre of string * TyreStatus option
    | ToggleMic
    | MicConnected
    | AudioDataReceived of byte[]
    | ModelUpdated of CarDescriptor
    | MessageAdded of string
    | ValidationRequested
