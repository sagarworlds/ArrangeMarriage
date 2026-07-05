import 'package:http/http.dart' as http;
import 'dart:convert';

class ApiService {
  final String baseUrl = 'http://localhost:5000/api';

  Future<http.Response> registerUser(Map<String, dynamic> user) async {
    return await http.post(
      Uri.parse('$baseUrl/user/register'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode(user),
    );
  }

  Future<http.Response> searchMatches(String userId) async {
    return await http.get(Uri.parse('$baseUrl/matchmaking/search/$userId'));
  }

  Future<http.Response> sendProposal(Map<String, dynamic> proposal) async {
    return await http.post(
      Uri.parse('$baseUrl/matchmaking/propose'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode(proposal),
    );
  }
}
