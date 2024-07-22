using GBX.NET.Engines.Game;
using GBX.NET;
using GbxIo.Components.Data;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.GameData;

namespace GbxIo.Components.Tools;

public sealed class ExtractMeshIoTool(string endpoint, IServiceProvider provider)
	: IoTool<Gbx, IEnumerable<TextData>>(endpoint, provider)
{
	public override string Name => "Extract mesh (OBJ+MTL)";

	public override Task<IEnumerable<TextData>> ProcessAsync(Gbx input)
	{
		var files = new List<TextData>();

		switch (input)
		{
			case Gbx<CPlugSolid> solid:
				using (var objWriter = new StringWriter())
				using (var mtlWriter = new StringWriter())
				{
					solid.Node.ExportToObj(objWriter, mtlWriter, 3);
					files.Add(new TextData(Path.GetFileNameWithoutExtension(input.FilePath) + ".obj", objWriter.ToString(), "obj"));
					files.Add(new TextData(Path.GetFileNameWithoutExtension(input.FilePath) + ".mtl", mtlWriter.ToString(), "mtl"));
				}
				break;
			case Gbx<CPlugPrefab> prefab:
				foreach (var ent in prefab.Node.Ents)
				{
					
				}
				break;
			case Gbx<CGameItemModel> itemModel:
				if (itemModel.Node.EntityModelEdition is CGameCommonItemEntityModelEdition { MeshCrystal: not null } edition)
				{
                    using var objWriter = new StringWriter();
                    using var mtlWriter = new StringWriter();

                    edition.MeshCrystal.ExportToObj(objWriter, mtlWriter, 3);
                    files.Add(new TextData(Path.GetFileNameWithoutExtension(input.FilePath) + ".obj", objWriter.ToString(), "obj"));
                    files.Add(new TextData(Path.GetFileNameWithoutExtension(input.FilePath) + ".mtl", mtlWriter.ToString(), "mtl"));
                }
				break;
			default:
				throw new InvalidOperationException("Only Item.Gbx, Block.Gbx, Solid.Gbx, and Prefab.Gbx is supported.");
		}

		return Task.FromResult(files.AsEnumerable());
	}
}