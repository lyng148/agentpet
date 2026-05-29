import XCTest
@testable import AgentPetCore

final class MoodResolverTests: XCTestCase {
    private func session(_ state: AgentState, id: String) -> AgentSession {
        AgentSession(id: id, agentKind: .claude, state: state, source: .hook,
                     updatedAt: Date(timeIntervalSince1970: 0))
    }

    func testEmptyIsIdle() {
        XCTAssertEqual(MoodResolver.aggregate([]), .idle)
    }

    func testWaitingWins() {
        let sessions = [session(.working, id: "a"), session(.waiting, id: "b"), session(.done, id: "c")]
        XCTAssertEqual(MoodResolver.aggregate(sessions), .waiting)
    }

    func testWorkingBeatsDone() {
        XCTAssertEqual(MoodResolver.aggregate([session(.done, id: "a"), session(.working, id: "b")]), .working)
    }

    func testRegisteredCountsAsWorking() {
        XCTAssertEqual(MoodResolver.aggregate([session(.registered, id: "a")]), .working)
    }

    func testDoneOnly() {
        XCTAssertEqual(MoodResolver.aggregate([session(.done, id: "a"), session(.idle, id: "b")]), .done)
    }
}

final class PetPackTests: XCTestCase {
    func testDecodeAndLookup() throws {
        let json = #"""
        {"name":"Test","version":1,"kind":"emoji",
         "states":{"idle":{"frames":["🐶"],"fps":1},"working":{"frames":["🐶","🦴"],"fps":2}}}
        """#
        let pack = try JSONDecoder().decode(PetPack.self, from: Data(json.utf8))
        XCTAssertEqual(pack.name, "Test")
        XCTAssertEqual(pack.kind, .emoji)
        XCTAssertEqual(pack.animation(for: .working).frames, ["🐶", "🦴"])
    }

    func testMissingMoodFallsBackToIdle() throws {
        let json = #"{"name":"T","version":1,"kind":"emoji","states":{"idle":{"frames":["🐶"],"fps":1}}}"#
        let pack = try JSONDecoder().decode(PetPack.self, from: Data(json.utf8))
        XCTAssertEqual(pack.animation(for: .celebrate).frames, ["🐶"], "missing mood uses idle")
        XCTAssertTrue(pack.missingMoods.contains(.working))
    }

    func testBuiltinsLoadAndAreComplete() {
        let packs = PetPackLoader.loadBuiltins()
        XCTAssertEqual(packs.map(\.name), ["Bear", "Cat", "Robot"])
        for pack in packs {
            XCTAssertEqual(pack.kind, .emoji)
            XCTAssertTrue(pack.missingMoods.isEmpty, "\(pack.name) missing \(pack.missingMoods)")
        }
    }
}
