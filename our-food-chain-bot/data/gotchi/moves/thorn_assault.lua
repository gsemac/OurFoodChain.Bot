function OnRegister(move)

	move.SetName("thorn assault")
	move.SetDescription("Assaults the opponent with thorns, dealing a large amount of random base damage.")

	move.SetPP(5)

	move.Requires.MinimumLevel(30).TypeMatch("producer").DescriptionMatch("\\b(?:thorn[sy]?|spike[sy]?|spiky|needles?)\\b")

end

function OnMove(args) 	
	
	args.DealDamage(rand(50, 100))

end