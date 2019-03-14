function register(move)

	move.name = "zap";
	move.description = "Shocks the opponent with electricity. Highly effective against aquatic organisms.";

	move.pp = 20;
	move.type = type.Offensive;

	move.requires.match = "electric|static";

end

function callback(args) 

	sql = "SELECT COUNT(*) FROM Zones WHERE type = \"aquatic\" AND id IN (SELECT zone_id FROM SpeciesZones WHERE species_id = $id);";

	if(args.TargetHasSql(sql)) then
		args.DoDamage(args.BaseDamage(), 1.2);
	else
		args.DoDamage();
	end

end