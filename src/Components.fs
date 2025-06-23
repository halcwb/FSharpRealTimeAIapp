namespace Components

open System
open Fable.Core
open Fable.Core.JsInterop
open Types

module TyreStatusPicker =

    [<JSX.Component>]
    let View (props: 
            {|
                value: TyreStatus option
                onChange: TyreStatus option -> unit
            |}) =

        let handleChange =
            fun ev ->
                let value = ev?target?value
                match value with
                | "NeedsReplacement" -> props.onChange (Some NeedsReplacement)
                | "Worn" -> props.onChange (Some Worn)
                | "Good" -> props.onChange (Some Good)
                | "New" -> props.onChange (Some New)
                | _ -> props.onChange None

        let selectedValue =
            match props.value with
            | Some NeedsReplacement -> "NeedsReplacement"
            | Some Worn -> "Worn"
            | Some Good -> "Good"
            | Some New -> "New"
            | None -> ""

        JSX.jsx
            $"""
        import FormControl from '@mui/material/FormControl';
        import InputLabel from '@mui/material/InputLabel';
        import Select from '@mui/material/Select';
        import MenuItem from '@mui/material/MenuItem';

        <FormControl fullWidth>
            <InputLabel>Status</InputLabel>
            <Select
                value={selectedValue}
                onChange={handleChange}
                label="Status"
            >
                <MenuItem value="">Select status</MenuItem>
                <MenuItem value="NeedsReplacement">Needs Replacement</MenuItem>
                <MenuItem value="Worn">Worn</MenuItem>
                <MenuItem value="Good">Good</MenuItem>
                <MenuItem value="New">New</MenuItem>
            </Select>
        </FormControl>
        """

module MicButton =

    [<JSX.Component>]
    let View (props:
            {|
                status: MicStatus
                onClick: unit -> unit
            |}) =

        let (color, iconName) = 
            match props.status with
            | Disconnected -> ("default", "Mic")
            | Active -> ("primary", "Mic")
            | Muted -> ("secondary", "MicOff")

        let handleClick = fun _ -> props.onClick()

        JSX.jsx
            $"""
        import IconButton from '@mui/material/IconButton';

        <IconButton 
            color={color}
            onClick={handleClick}
        >
            {if iconName = "MicOff" then Mui.Icons.MicOffIcon else Mui.Icons.MicIcon}
        </IconButton>
        """