import tempfile
import unittest
from pathlib import Path

import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "scripts"))

import ltrain


class LTrainTests(unittest.TestCase):
    def test_parse_args_uses_required_paths_and_defaults(self):
        cfg = ltrain.parse_args(["--dataset", "data.jsonl", "--model", "model.gguf", "--output", "out", "--dry-run"])
        self.assertEqual(cfg.device, "cpu")
        self.assertEqual(cfg.epochs, 1)
        self.assertTrue(cfg.dry_run)

    def test_validate_rejects_missing_dataset_and_model(self):
        with tempfile.TemporaryDirectory() as tmp:
            cfg = ltrain.LTrainConfig(
                dataset=Path(tmp) / "missing-data.jsonl",
                model=Path(tmp) / "missing-model.gguf",
                output=Path(tmp) / "out",
                device="cpu",
                epochs=1,
                batch_size=1,
                learning_rate=0.0001,
                min_disk_gb=0,
                trainer=None,
                dry_run=True,
            )
            with self.assertRaisesRegex(ltrain.ValidationError, "dataset, model"):
                ltrain.validate_config(cfg)

    def test_validate_accepts_cpu_dry_run_configuration(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            dataset = root / "data.jsonl"
            model = root / "model.gguf"
            dataset.write_text('{"prompt":"hi","completion":"there"}\n', encoding="utf-8")
            model.write_text("placeholder", encoding="utf-8")
            cfg = ltrain.parse_args([
                "--dataset", str(dataset),
                "--model", str(model),
                "--output", str(root / "out"),
                "--min-disk-gb", "0",
                "--dry-run",
            ])
            ltrain.validate_config(cfg)
            self.assertTrue((root / "out").is_dir())

    def test_validate_rejects_invalid_training_numbers(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            dataset = root / "data.jsonl"
            model = root / "model.gguf"
            dataset.touch()
            model.touch()
            cfg = ltrain.LTrainConfig(dataset, model, root / "out", "cpu", 0, 1, 0.1, 0, None, True)
            with self.assertRaisesRegex(ltrain.ValidationError, "epochs"):
                ltrain.validate_config(cfg)


if __name__ == "__main__":
    unittest.main()
