function OnRegister(move)

	move.SetName("thorny overgrowth")
	move.SetDescription("Grows thorny structures surrounding the opponent, causing them to take damage every time they attack.")

	move.SetPP(10)

	move.Requires.TypeMatch("producer").MinimumLevel(20).DescriptionMatch("thorn")

end

function OnMove(args) 
	
	args.Target.SetStatus("thorn-surrounded")

	args.SetText("surrounding the opponent with thorns")

end