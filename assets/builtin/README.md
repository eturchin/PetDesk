# Original built-in pets

Mochi Cat and Pixel Fox were drawn from original vector/pixel geometry for PetDesk. No third-party character images were used. The artwork is covered by the repository's MIT license.

The source is `tools/PetDesk.AssetBuilder/Program.cs`. Each PNG is a transparent 1152×624 atlas with 192×208 cells. The first row contains four idle frames including a blink; the next two contain six walking frames in each direction. Each pet has an ordinary schema-1 pet.json and uses the same loader/player as imported pets.

Rebuild with `dotnet run --project tools/PetDesk.AssetBuilder -c Release -- assets/builtin`.
