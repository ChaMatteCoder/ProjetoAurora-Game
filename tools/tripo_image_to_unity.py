import argparse
import asyncio
import os
from pathlib import Path

from tripo3d import TripoClient


def task_status_value(task) -> str:
    status = getattr(task, "status", "")
    return getattr(status, "value", str(status)).lower()


async def main():
    parser = argparse.ArgumentParser(
        description="Generate a Unity-ready 3D asset from an image using Tripo."
    )

    parser.add_argument("--image", required=True, help="Path to reference image.")
    parser.add_argument("--name", required=True, help="Asset name, e.g. Aurora_Box_01.")
    parser.add_argument(
        "--project",
        default=r"C:\ProjetoAuroraGame",
        help="Unity project root path.",
    )
    parser.add_argument(
        "--face-limit",
        type=int,
        default=6000,
        help="Target face limit for game-ready obstacle.",
    )

    args = parser.parse_args()

    api_key = os.getenv("TRIPO_API_KEY")
    if not api_key:
        raise RuntimeError(
            "TRIPO_API_KEY was not found. Set it with: setx TRIPO_API_KEY \"your_key\""
        )

    image_path = Path(args.image)
    if not image_path.exists():
        raise FileNotFoundError(f"Image not found: {image_path}")

    project_root = Path(args.project)
    output_dir = (
        project_root
        / "Assets"
        / "_ProjectAurora"
        / "Art"
        / "Generated"
        / "Obstacles"
        / args.name
    )
    output_dir.mkdir(parents=True, exist_ok=True)

    async with TripoClient(api_key=api_key) as client:
        print(f"[Tripo] Generating model from image: {image_path}")

        task_id = await client.image_to_model(
            image=str(image_path),
            model_version="v2.5-20250123",
            face_limit=args.face_limit,
            texture=True,
            pbr=True,
            texture_quality="standard",
            texture_alignment="original_image",
            compress=True,
            smart_low_poly=True,
        )

        print(f"[Tripo] Task created: {task_id}")
        print("[Tripo] Waiting for completion...")

        task = await client.wait_for_task(
            task_id,
            polling_interval=3.0,
            timeout=600,
            verbose=True,
        )

        status = task_status_value(task)
        if "success" not in status:
            raise RuntimeError(f"Tripo task did not finish successfully. Status: {status}")

        print("[Tripo] Downloading generated model files...")
        downloaded = await client.download_task_models(task, str(output_dir))

        print("[Tripo] Done.")
        for model_type, file_path in downloaded.items():
            if file_path:
                print(f"{model_type}: {file_path}")

        print(f"[Unity] Files saved inside: {output_dir}")
        print("[Unity] Reopen/focus Unity so it imports the generated asset.")


if __name__ == "__main__":
    asyncio.run(main())