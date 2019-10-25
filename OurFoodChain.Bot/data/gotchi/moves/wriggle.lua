function OnRegister(move)

	move.SetName("wriggle")
	move.SetDescription("The user uselessly wriggles like a worm.")

	move.SetPP(40)
	
	move.Requires.MaximumLevel(10).DescriptionMatch("worm")

end

function OnMove(args)
end