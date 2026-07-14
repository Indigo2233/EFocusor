import io
import unittest
import xml.etree.ElementTree as ET
from contextlib import redirect_stdout

from indi_efocuser_focuser import DEVICE, EFocuserINDI, SerialIO


class FakeSerial:
    def __init__(self):
        self.moves = []

    def move(self, position):
        self.moves.append(position)
        return True


class DriverProtocolTests(unittest.TestCase):
    def setUp(self):
        self.driver = EFocuserINDI()

    def output_for(self, xml):
        output = io.StringIO()
        with redirect_stdout(output):
            self.driver.parse(xml)
        result = output.getvalue()
        ET.fromstring(f"<output>{result}</output>")
        return result

    def test_get_properties_redefines_base_properties(self):
        output = self.output_for('<getProperties version="1.7"/>')

        self.assertIn('name="DRIVER_INFO"', output)
        self.assertIn('name="DEVICE_PORT"', output)
        self.assertIn('<oneText name="PORT"', output)
        self.assertIn('name="CONNECTION"', output)

    def test_connected_get_properties_defines_standard_focuser_properties(self):
        self.driver.conn = True

        output = self.output_for(
            f'<getProperties version="1.7" device="{DEVICE}"/>'
        )

        expected = (
            'FOCUS_SPEED',
            'FOCUS_MOTION',
            'ABS_FOCUS_POSITION',
            'REL_FOCUS_POSITION',
            'FOCUS_MAX',
            'FOCUS_ABORT_MOTION',
            'FOCUS_REVERSE_MOTION',
            'FOCUS_TEMPERATURE',
        )
        for name in expected:
            self.assertIn(f'name="{name}"', output)

        self.assertIn('<oneNumber name="FOCUS_SPEED_VALUE"', output)
        self.assertIn('<oneNumber name="FOCUS_ABSOLUTE_POSITION"', output)
        self.assertIn('<oneNumber name="FOCUS_RELATIVE_POSITION"', output)
        self.assertIn('<oneNumber name="FOCUS_MAX_VALUE"', output)
        self.assertIn('<oneNumber name="TEMPERATURE"', output)

    def test_named_get_properties_only_returns_requested_property(self):
        self.driver.conn = True

        output = self.output_for(
            f'<getProperties version="1.7" device="{DEVICE}" '
            'name="ABS_FOCUS_POSITION"/>'
        )

        self.assertIn('name="ABS_FOCUS_POSITION"', output)
        self.assertNotIn('name="FOCUS_SPEED"', output)
        self.assertNotIn('name="CONNECTION"', output)

    def test_standard_absolute_position_command_moves_focuser(self):
        serial = FakeSerial()
        self.driver.ser = serial
        self.driver.conn = True

        self.output_for(
            f'<newNumberVector device="{DEVICE}" name="ABS_FOCUS_POSITION">'
            '<oneNumber name="FOCUS_ABSOLUTE_POSITION">1234</oneNumber>'
            '</newNumberVector>'
        )

        self.assertEqual([1234], serial.moves)

    def test_standard_relative_position_uses_motion_direction(self):
        serial = FakeSerial()
        self.driver.ser = serial
        self.driver.conn = True
        self.driver.pos = 1000
        self.driver.inward = True

        self.output_for(
            f'<newNumberVector device="{DEVICE}" name="REL_FOCUS_POSITION">'
            '<oneNumber name="FOCUS_RELATIVE_POSITION">250</oneNumber>'
            '</newNumberVector>'
        )

        self.assertEqual([750], serial.moves)

    def test_relative_move_returns_to_ok_when_poll_reports_completion(self):
        self.driver.active_motion = "REL_FOCUS_POSITION"
        self.driver.active_motion_value = 250

        output = io.StringIO()
        with redirect_stdout(output):
            self.driver._publish_motion_status(750, False)

        result = output.getvalue()
        ET.fromstring(f"<output>{result}</output>")
        self.assertIn('name="ABS_FOCUS_POSITION" state="Ok"', result)
        self.assertIn('name="REL_FOCUS_POSITION" state="Ok"', result)
        self.assertIsNone(self.driver.active_motion)

    def test_missing_status_response_does_not_report_a_false_zero(self):
        serial = SerialIO(self.driver.log)
        serial.cmd = lambda command: None

        self.assertIsNone(serial.status())


if __name__ == "__main__":
    unittest.main()
