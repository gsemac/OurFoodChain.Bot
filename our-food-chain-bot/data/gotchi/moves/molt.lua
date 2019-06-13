function register(move)

	move.name = "molt";
	move.description = "Molts exoskeleton, undoing all stat changes and restoring HP.";

	move.pp = 5;
	move.type = type.Recovery;

	move.requires.match = "molt";

end

function callback(args) 

	args.text = "resetting all stat changes and restoring HP";

	args.user.stats.Reset();
	args.user.status = "";
	args.DoRecoverPercent(0.2);

end