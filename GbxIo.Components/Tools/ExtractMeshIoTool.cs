﻿using GBX.NET.Engines.Game;
using GBX.NET;
using GbxIo.Components.Data;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.GameData;

namespace GbxIo.Components.Tools;

public sealed class ExtractMeshIoTool(string endpoint, IServiceProvider provider)
	: IoTool<Gbx, IEnumerable<TextData>>(endpoint, provider)
{
	public override string Name => "Extract mesh (OBJ+MTL)";

	public override Task<IEnumerable<TextData>> ProcessAsync(Gbx input, CancellationToken cancellationToken)
	{
		var files = new List<TextData>();

		switch (input)
		{
			case Gbx<CPlugSolid> solid:
				using (var objWriter = new StringWriter())
				using (var mtlWriter = new StringWriter())
				{
					solid.Node.ExportToObj(objWriter, mtlWriter, 3);
					files.Add(new TextData(input.FilePath + ".obj", objWriter.ToString(), "obj"));
					files.Add(new TextData(input.FilePath + ".mtl", mtlWriter.ToString(), "mtl"));
				}
				break;
			case Gbx<CPlugPrefab> prefab:
                for (int i = 0; i < prefab.Node.Ents.Length; i++)
				{
                    var ent = prefab.Node.Ents[i];

					if (ent.Model is not CPlugStaticObjectModel staticObject)
					{
						continue;
					}

                    if (staticObject.Mesh is null)
                    {
						continue;
                    }

                    using var objWriter = new StringWriter();
                    using var mtlWriter = new StringWriter();

                    staticObject.Mesh.ExportToObj(objWriter, mtlWriter, 3);
                    files.Add(new TextData(input.FilePath + ".obj", objWriter.ToString(), "obj"));
                    files.Add(new TextData(input.FilePath + ".mtl", mtlWriter.ToString(), "mtl"));
                }
				break;
			case Gbx<CGameItemModel> itemModel:
				if (itemModel.Node.EntityModelEdition is CGameCommonItemEntityModelEdition { MeshCrystal: not null } edition)
				{
                    using var objWriter = new StringWriter();
                    using var mtlWriter = new StringWriter();

                    edition.MeshCrystal.ExportToObj(objWriter, mtlWriter, 3);
                    files.Add(new TextData(input.FilePath + ".obj", objWriter.ToString(), "obj"));
                    files.Add(new TextData(input.FilePath + ".mtl", mtlWriter.ToString(), "mtl"));
                }
				else if (itemModel.Node.EntityModelEdition is CGameBlockItem block)
				{
					foreach (var variant in block.CustomizedVariants)
                    {
						if (variant.Crystal is null)
						{
							continue;
						}

                        using var objWriter = new StringWriter();
                        using var mtlWriter = new StringWriter();

						var newFileName = $"{GbxPath.GetFileNameWithoutExtension(input.FilePath)}_{variant.Id}.Block.Gbx";
						var directoryPath = Path.GetDirectoryName(input.FilePath);
						if (!string.IsNullOrEmpty(directoryPath))
						{
							newFileName = Path.Combine(directoryPath, newFileName);
                        }

                        variant.Crystal.ExportToObj(objWriter, mtlWriter, 3);
                        files.Add(new TextData($"{newFileName}.obj", objWriter.ToString(), "obj"));
                        files.Add(new TextData($"{newFileName}.mtl", mtlWriter.ToString(), "mtl"));
                    }
				}
				else if (itemModel.Node.EntityModel is CGameCommonItemEntityModel { StaticObject.Mesh: not null } model)
				{
                    using var objWriter = new StringWriter();
                    using var mtlWriter = new StringWriter();

                    model.StaticObject.Mesh.ExportToObj(objWriter, mtlWriter, 3);
                    files.Add(new TextData(input.FilePath + ".obj", objWriter.ToString(), "obj"));
                    files.Add(new TextData(input.FilePath + ".mtl", mtlWriter.ToString(), "mtl"));
                }
				else
                {
                    throw new Exception("Item has no mesh that would be supported.");
                }
				break;
			case Gbx<CPlugSolid2Model> solid2:
				using (var objWriter = new StringWriter())
				using (var mtlWriter = new StringWriter())
				{
                    solid2.Node.ExportToObj(objWriter, mtlWriter, 3);
					files.Add(new TextData(input.FilePath + ".obj", objWriter.ToString(), "obj"));
					files.Add(new TextData(input.FilePath + ".mtl", mtlWriter.ToString(), "mtl"));
				}
				break;
            default:
				throw new InvalidOperationException("Only Item.Gbx, Block.Gbx, Mesh.Gbx, Solid.Gbx, and Prefab.Gbx is supported.");
		}

		return Task.FromResult(files.AsEnumerable());
	}
}