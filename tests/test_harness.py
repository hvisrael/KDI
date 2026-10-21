import importlib.util, pathlib, unittest
P=pathlib.Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('harness',P/'scripts'/'harness.py')
h=importlib.util.module_from_spec(spec);spec.loader.exec_module(h)
class HarnessTests(unittest.TestCase):
    def test_forbidden(self):
        for v in ('../IDI/file','IDI/foo','C:/evil','/tmp/foo','secrets/key','.env/file'):
            with self.assertRaises(ValueError): h.check_path(v)
    def test_allowed(self):
        self.assertTrue(h.within_allowed('docs/demo.md',['docs/']))
        self.assertFalse(h.within_allowed('docs2/demo.md',['docs/']))
    def test_missing_tests(self):
        t=h.load_json(P/'examples'/'task.example.json');t['test_commands']=[]
        with self.assertRaises(ValueError): h.validate_task(t)
if __name__=='__main__': unittest.main()
