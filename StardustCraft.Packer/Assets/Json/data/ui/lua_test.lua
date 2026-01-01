function init()
    print("Panel init")
end

function update(dt)
    local fps = ui:GetGameInstance().currentFps
    ui.text = "FPS: "..fps
end