module Components

open Feliz
open Feliz.MaterialUI
open Types

let tyreStatusPicker (value: TyreStatus option) (onChange: TyreStatus option -> unit) =
    Mui.formControl [
        formControl.fullWidth true
        formControl.children [
            Mui.inputLabel "Status"
            Mui.select [
                select.value (
                    match value with
                    | Some NeedsReplacement -> "NeedsReplacement"
                    | Some Worn -> "Worn"
                    | Some Good -> "Good"
                    | Some New -> "New"
                    | None -> ""
                )
                select.onChange (fun (e: Event) ->
                    let value = e.target?value
                    match value with
                    | "NeedsReplacement" -> onChange (Some NeedsReplacement)
                    | "Worn" -> onChange (Some Worn)
                    | "Good" -> onChange (Some Good)
                    | "New" -> onChange (Some New)
                    | _ -> onChange None
                )
                select.children [
                    Mui.menuItem [
                        menuItem.value ""
                        menuItem.children "Select status"
                    ]
                    Mui.menuItem [
                        menuItem.value "NeedsReplacement"
                        menuItem.children "Needs Replacement"
                    ]
                    Mui.menuItem [
                        menuItem.value "Worn"
                        menuItem.children "Worn"
                    ]
                    Mui.menuItem [
                        menuItem.value "Good"
                        menuItem.children "Good"
                    ]
                    Mui.menuItem [
                        menuItem.value "New"
                        menuItem.children "New"
                    ]
                ]
            ]
        ]
    ]

let micButton (status: MicStatus) (onClick: unit -> unit) =
    let (color, icon) = 
        match status with
        | Disconnected -> ("default", "mic")
        | Active -> ("primary", "mic")
        | Muted -> ("secondary", "mic_off")
    
    Mui.iconButton [
        iconButton.color color
        iconButton.onClick (fun _ -> onClick())
        iconButton.children [
            Mui.icon icon
        ]
    ]